module Workflows.ClientCredentialsWorkflow

open FsToolkit.ErrorHandling
open RhinoAuth.Database
open Settings
open System
open Utilities

open Repository
open WorkflowBuilder


type ClientCredentialsFlowData = {
    WorkflowData: WorkflowData
    // Although this is a form model (FormModel), I have to name it RequestModel so I can use Duck Typing
    RequestModel: OAuthModels.TokenRequest
    ApiClient: ApiClient
}


let inline private fetchClient wd (formModel: OAuthModels.TokenRequest) =
    wd.DbQueries.GetApiClient formModel.client_id (formModel.client_secret |> Option.defaultValue "")
    |> Task.map (function
        | None -> Error (OAuthModels.ProtocolError.invalid_request, "Invalid client id or secret")
        | Some client -> Ok { WorkflowData = wd; RequestModel = formModel; ApiClient = client }
    )


let inline private generateTokenRequest flowData = task {

    let! jwk = flowData.WorkflowData.DbQueries.GetJWK()

    let tokenRequestId = KeyGenerator.getString32To64Chars()

    let issuer = flowData.WorkflowData.AppBehavior.OAuthIssuer

    let scopes =
        match flowData.RequestModel.scope with
        | Some value -> ResizeArray(value.Split ' ')
        | None -> ResizeArray()
    
    let resources =
        flowData.RequestModel.resource
        |> Option.map (fun list -> list |> List.map (fun id -> flowData.ApiClient.ApiClientResources |> Seq.find (fun cr -> cr.ApiResourceId = id)))
        |> Option.map (fun list -> list |> List.map (fun resource -> TokenRequestApiResource(TokenRequestId = tokenRequestId, ApiResourceId = resource.ApiResourceId)))
        |> Option.map ResizeArray
        |> Option.defaultValue (ResizeArray())

    let audiences = [|
        for ar in resources do
            yield ar.ApiResourceId

        if scopes.Contains AuthorizeWorkflow.rhinoIdentityScope then
            yield issuer
    |]

    let accessToken = TokenGenerator.getAccessTokenOfClient flowData.ApiClient issuer audiences None jwk
    let refreshToken = TokenGenerator.getRefreshToken ()

    let tokenRequest = ApiClientTokenRequest(
        Id = tokenRequestId,
        IpAddress = flowData.WorkflowData.IpAddress,
        AccessToken = accessToken,
        RefreshToken = refreshToken,
        Scopes = scopes,
        ApiClientId = flowData.ApiClient.Id,
        TokenRequestApiResources = resources
    )

    return Ok (tokenRequest, flowData)
}


// This will be reused
let inline mapClientCredentialsDbResult accessToken refreshToken command =
    command
    |> TaskResult.map (fun _ ->
        ({
            access_token = accessToken
            id_token = null
            refresh_token = refreshToken
            expires_in = DateTimeOffset.UtcNow.Ticks
            is_persistent = false
            token_type = "Bearer"
        }: OAuthModels.TokenSuccessResponse)
    )


let inline private updateDbAndGetResult ((tokenRequest: ApiClientTokenRequest), flowData) =

    flowData.WorkflowData.DbCommands.CreateRow tokenRequest
    |> mapClientCredentialsDbResult tokenRequest.AccessToken tokenRequest.RefreshToken
    |> AuthorizationCodeWorkflow.mapDefaultOAuthDbError


let entry wd (formModel: OAuthModels.TokenRequest) =
    formModel
    |> fetchClient wd
    |> TaskResult.bind (toTask AuthorizeWorkflow.checkClientIsVerified)
    |> TaskResult.bind (toTask AuthorizeWorkflow.checkClientIsActive)
    |> TaskResult.bind (toTask AuthorizeWorkflow.checkOAuthResource)
    |> TaskResult.bind (toTask AuthorizeWorkflow.checkScopes)
    |> TaskResult.bind generateTokenRequest
    |> TaskResult.bind updateDbAndGetResult