module Workflows.AuthorizationCodeWorkflow

open FsToolkit.ErrorHandling
open RhinoAuth.Database
open Settings
open System
open Utilities

open Repository
open WorkflowBuilder


type private AuthorizationCodeFlowData = {
    WorkflowData: WorkflowData
    FormModel: OAuthModels.TokenRequest
    AuthorizeRequest: AuthorizeRequest
}


let inline private fetchAuthorizationRequest wd (formModel: OAuthModels.TokenRequest) =
    wd.DbQueries.GetAuthorizeRequest (formModel.code |> Option.defaultValue "") (wd.User.GetRequiredId())
    |> Task.map (function
        | None -> Error (OAuthModels.ProtocolError.invalid_request, "Invalid code")
        | Some ar -> Ok { WorkflowData = wd; FormModel = formModel; AuthorizeRequest = ar }
    )


let inline private checkConfidentialClient flowData =
    match (flowData.AuthorizeRequest.ApiClient.Type, flowData.FormModel.client_secret) with
    | (ApiClientType.Confidential, None) -> Error (OAuthModels.ProtocolError.invalid_client, "client_secret is required for this client")
    | (ApiClientType.Confidential, Some secret) when secret <> flowData.AuthorizeRequest.ApiClient.Secret -> Error (OAuthModels.ProtocolError.invalid_client, "Invalid client_id or client_secret")
    | _ -> Ok flowData
    |> Task.singleton


let inline private checkRequestIsConsented flowData =
    match flowData.AuthorizeRequest.ConsentedAt.HasValue with
    | false -> Error (OAuthModels.ProtocolError.access_denied, "This request is not consented")
    | _ -> Ok flowData
    |> Task.singleton


let inline private checkExpiration flowData =
    match flowData.AuthorizeRequest.CreatedAt.AddMinutes OAuthModels.expirationInMinutes < DateTimeOffset.UtcNow with
    | true -> Error (OAuthModels.ProtocolError.invalid_request, "This request has expired")
    | _ -> Ok flowData
    |> Task.singleton


let inline private checkCodeVerifier flowData =
    match (flowData.FormModel.code_verifier, flowData.AuthorizeRequest.VerifierMethod) with
    | (None, _) -> Error (OAuthModels.ProtocolError.invalid_request, "code_verifier is required")

    | (Some code, VerifierMethod.S256) when flowData.AuthorizeRequest.CodeChallenge <> TokenGenerator.getSha256 code ->
        Error (OAuthModels.ProtocolError.unauthorized_client, "Incorrect code_verifier")
    
    | (Some code, VerifierMethod.Plain) when flowData.AuthorizeRequest.CodeChallenge <> code ->
        Error (OAuthModels.ProtocolError.invalid_request, "Incorrect code_verifier")
    
    | _ -> Ok flowData
    |> Task.singleton


let inline private generateTokensFromAuthorizationRequest flowData = task {

    let! jwk = flowData.WorkflowData.DbQueries.GetJWK()
    let issuer = flowData.WorkflowData.AppBehavior.OAuthIssuer
    let audiences = [|
        for ar in flowData.AuthorizeRequest.AuthorizeRequestApiResources do
            yield ar.ApiResourceId

        if flowData.AuthorizeRequest.Scopes.Contains AuthorizeWorkflow.rhinoIdentityScope then
            yield issuer
    |]

    let accessToken =
        match flowData.AuthorizeRequest.RequestType with
        | AuthorizeRequestType.OpenId -> None
        | _ -> Some <| TokenGenerator.getAccessTokenOfUser flowData.AuthorizeRequest.User flowData.AuthorizeRequest.LoginId issuer audiences None jwk

    let idToken =
        match flowData.AuthorizeRequest.RequestType with
        | AuthorizeRequestType.OAuth -> None
        | _ -> Some <| TokenGenerator.getIdToken flowData.AuthorizeRequest.User flowData.AuthorizeRequest.LoginId issuer (List.ofSeq flowData.AuthorizeRequest.Scopes) audiences (Option.ofObj flowData.AuthorizeRequest.Nonce) jwk
    
    let refreshToken = TokenGenerator.getRefreshToken ()

    return Ok (accessToken, idToken, refreshToken, flowData)
}


// This will be reused
let inline mapDefaultOAuthDbError command =
    command
    |> TaskResult.mapError (function
        | ConcurrentDataAccess -> (OAuthModels.ProtocolError.temporarily_unavailable, "Concurrent requests are not supported")
        | _ -> (OAuthModels.ProtocolError.server_error, "Failed to process the request due to internal error")
    )


let inline private updateAuthorizationRequest (accessToken, idToken, refreshToken, flowData) =

    let externalLoginId = KeyGenerator.getString32To64Chars()

    flowData.AuthorizeRequest.CompletedAt <- DateTimeOffset.UtcNow

    let externalLogin = ExternalLogin(
        Id = externalLoginId,
        AccessToken = Option.toObj accessToken,
        IdToken = Option.toObj idToken,
        RefreshToken = refreshToken,
        ApiClientId = flowData.AuthorizeRequest.ApiClientId,
        IpAddress = flowData.WorkflowData.IpAddress,
        LoginId = flowData.AuthorizeRequest.LoginId,
        UserId = flowData.AuthorizeRequest.UserId,
        UserAgent = (flowData.WorkflowData.HeaderProvider Microsoft.Net.Http.Headers.HeaderNames.UserAgent |> Option.toObj),
        ExternalLoginApiResources =
            (flowData.AuthorizeRequest.AuthorizeRequestApiResources
            |> Seq.map (fun ar ->
                ExternalLoginApiResource(
                    ExternalLoginId = externalLoginId,
                    ApiResourceId = ar.ApiResourceId
                )
            )
            |> ResizeArray)
    )

    flowData.WorkflowData.DbCommands.CreateRow externalLogin
    |> TaskResult.map (fun _ ->
        ({
            access_token = Option.toObj accessToken
            id_token = Option.toObj idToken
            refresh_token = refreshToken
            expires_in = DateTimeOffset.UtcNow.Ticks
            is_persistent = flowData.AuthorizeRequest.Login.IsPersistent
            token_type = "Bearer"
        }: OAuthModels.TokenSuccessResponse)
    )
    |> mapDefaultOAuthDbError


let entry wd (formModel: OAuthModels.TokenRequest) =
    formModel
    |> fetchAuthorizationRequest wd
    |> TaskResult.bind checkConfidentialClient
    |> TaskResult.bind checkRequestIsConsented
    |> TaskResult.bind checkExpiration
    |> TaskResult.bind checkCodeVerifier
    |> TaskResult.bind generateTokensFromAuthorizationRequest
    |> TaskResult.bind updateAuthorizationRequest