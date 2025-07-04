module Workflows.RefreshTokenWorkflow

open FsToolkit.ErrorHandling
open RhinoAuth.Database
open Settings
open System
open Utilities

open Repository
open WorkflowBuilder


let inline private checkClientSecret (formModel: OAuthModels.TokenRequest) (tokenRequest: ApiClientTokenRequest) =
    match formModel.client_secret with
    | Some secret when secret = tokenRequest.ApiClient.Secret -> Ok tokenRequest
    | _ -> Error (OAuthModels.ProtocolError.access_denied, "Invalid client id or secret")


// Ideally this should come from app settings
let inline private checkTokenIsExpired (tokenRequest: ApiClientTokenRequest) =
    match tokenRequest.CreatedAt.AddDays 30 < DateTimeOffset.UtcNow with
    | true -> Error (OAuthModels.ProtocolError.invalid_request, "Token is expired")
    | _ -> Ok tokenRequest


let inline private checkTokenRequestIsUsed (tokenRequest: ApiClientTokenRequest) =
    match tokenRequest.IsRefreshTokenUsed with
    | true -> Error (OAuthModels.ProtocolError.invalid_request, "Token is used")
    | _ -> Ok tokenRequest


let inline private generateNewTokens wd (tokenRequest: ApiClientTokenRequest) = task {

    let! jwk = wd.DbQueries.GetJWK()

    let tokenRequestId = KeyGenerator.getString32To64Chars()

    let issuer = wd.AppBehavior.OAuthIssuer

    let audiences = [|
        for ar in tokenRequest.TokenRequestApiResources do
            yield ar.ApiResourceId

        if tokenRequest.Scopes.Contains AuthorizeWorkflow.rhinoIdentityScope then
            yield issuer
    |]

    let accessToken = TokenGenerator.getAccessTokenOfClient tokenRequest.ApiClient issuer audiences None jwk
    let refreshToken = TokenGenerator.getRefreshToken ()

    let newTokenRequest = ApiClientTokenRequest(
        Id = tokenRequestId,
        IpAddress = wd.IpAddress,
        AccessToken = accessToken,
        RefreshToken = refreshToken,
        RefreshedBy = tokenRequest.RefreshToken,
        Scopes = tokenRequest.Scopes,
        ApiClientId = tokenRequest.ApiClientId,
        TokenRequestApiResources = tokenRequest.TokenRequestApiResources
    )

    return Ok (tokenRequest, newTokenRequest)
}


let inline private updateDbAndGetResult wd (tokenRequest: ApiClientTokenRequest, newTokenRequest: ApiClientTokenRequest) =
    
    tokenRequest.IsRefreshTokenUsed <- true

    wd.DbCommands.CreateRow newTokenRequest
    |> ClientCredentialsWorkflow.mapClientCredentialsDbResult newTokenRequest.AccessToken newTokenRequest.RefreshToken
    |> AuthorizationCodeWorkflow.mapDefaultOAuthDbError


let inline private clientCredentialsEntry wd (formModel: OAuthModels.TokenRequest) (tokenRequest: ApiClientTokenRequest) =
    tokenRequest
    |> checkClientSecret formModel
    |> Result.bind checkTokenIsExpired
    |> Result.bind checkTokenRequestIsUsed
    |> TaskResult.ofResult
    |> TaskResult.bind (generateNewTokens wd)
    |> TaskResult.bind (updateDbAndGetResult wd)





let inline private checkConfidentialClient (formModel: OAuthModels.TokenRequest) (externalLogin: ExternalLogin) =
    match (externalLogin.ApiClient.Type, formModel.client_secret) with
    | (ApiClientType.Confidential, None) -> Error (OAuthModels.ProtocolError.invalid_client, "client_secret is required for this client")
    | (ApiClientType.Confidential, Some secret) when secret <> externalLogin.ApiClient.Secret -> Error (OAuthModels.ProtocolError.invalid_client, "Invalid client_id or client_secret")
    | _ -> Ok (formModel, externalLogin)


let inline private checkSessionIsValid (formModel: OAuthModels.TokenRequest, externalLogin: ExternalLogin) =
    match externalLogin.Login.IsValid() with
    | false -> Error (OAuthModels.ProtocolError.invalid_request, "This session has been ended")
    | _ -> Ok (formModel, externalLogin)


let inline private checkRefreshTokenIsUsed (formModel: OAuthModels.TokenRequest, externalLogin: ExternalLogin) =
    match externalLogin.PreviousRefreshToken = formModel.refresh_token.Value with
    | true -> Error (OAuthModels.ProtocolError.invalid_request, "This token has already been used")
    | _ -> Ok externalLogin


let inline private updateExternalLogin wd (externalLogin: ExternalLogin) = task {

    let! jwk = wd.DbQueries.GetJWK()

    let issuer = wd.AppBehavior.OAuthIssuer

    let audiences = [|
        for ar in externalLogin.ExternalLoginApiResources do
            yield ar.ApiResourceId

        if externalLogin.OpenIdScopes.Contains AuthorizeWorkflow.rhinoIdentityScope then
            yield issuer
    |]

    let accessToken =
        match Option.ofObj externalLogin.AccessToken with
        | None -> null
        | Some _ -> TokenGenerator.getAccessTokenOfUser externalLogin.Login.User externalLogin.LoginId issuer audiences None jwk

    let idToken =
        match Option.ofObj externalLogin.IdToken with
        | None -> null
        | Some _ -> TokenGenerator.getIdToken externalLogin.Login.User externalLogin.LoginId issuer (List.ofSeq externalLogin.OpenIdScopes) audiences None jwk

    let refreshToken = TokenGenerator.getRefreshToken ()

    externalLogin.PreviousRefreshToken <- externalLogin.RefreshToken

    externalLogin.AccessToken <- accessToken
    externalLogin.IdToken <- idToken
    externalLogin.RefreshToken <- refreshToken
    externalLogin.UpdatedAt <- DateTimeOffset.UtcNow

    return!
        wd.DbCommands.UpdateRow externalLogin
        |> TaskResult.map (fun _ ->
            ({
                access_token = accessToken
                id_token = idToken
                refresh_token = refreshToken
                expires_in = DateTimeOffset.UtcNow.Ticks
                is_persistent = externalLogin.Login.IsPersistent
                token_type = "Bearer"
            }: OAuthModels.TokenSuccessResponse)
        )
        |> AuthorizationCodeWorkflow.mapDefaultOAuthDbError
}


let inline private openIdEntry wd (formModel: OAuthModels.TokenRequest) (externalLogin: ExternalLogin) =
    externalLogin
    |> checkConfidentialClient formModel
    |> Result.bind checkSessionIsValid
    |> Result.bind checkRefreshTokenIsUsed
    |> TaskResult.ofResult
    |> TaskResult.bind (updateExternalLogin wd)





let inline private checkRefreshToken (formModel: OAuthModels.TokenRequest) =
    match formModel.refresh_token with
    | None -> Error (OAuthModels.ProtocolError.invalid_request, "refresh_token is required for this flow")
    | Some refreshToken -> Ok (formModel.client_id, refreshToken)


let inline private fetchRefreshTokenDbData wd (clientId, refreshToken) =
    wd.DbQueries.GetRefreshTokenData clientId refreshToken
    |> Task.map (function
        | None -> Error (OAuthModels.ProtocolError.invalid_request, "Invalid client_id or refresh_token")
        | Some refreshData -> Ok refreshData
    )


let entry wd (formModel: OAuthModels.TokenRequest) =
    formModel
    |> (toTask checkRefreshToken)
    |> TaskResult.bind (fetchRefreshTokenDbData wd)
    |> TaskResult.bind (function
        | ForClientCredentials tokenRequest -> clientCredentialsEntry wd formModel tokenRequest
        | ForOpenId externalLogin -> openIdEntry wd formModel externalLogin
    )