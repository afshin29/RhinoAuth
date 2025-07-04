module Workflows.AuthorizeWorkflow

open FsToolkit.ErrorHandling
open RhinoAuth.Database
open Settings
open Utilities

open Repository
open WorkflowBuilder


// Since protocol errors are intended for programmers, they are not translated


let openidScopes = [| "openid"; "profile"; "email"; "phone"; "address"; "offline_access" |]

[<Literal>]
let rhinoIdentityScope = "identity"



type AuthorizeResponse =
    | ShowConsent of code: string
    | Redirect of code: string
    | RedirectWithState of code: string * state: string



type AuthorizeFlowData = {
    WorkflowData: WorkflowData
    RequestModel: OAuthModels.AuthorizeRequest
    RequestType: AuthorizeRequestType
    ApiClient: ApiClient
    UnfinishedRequestsCount: int
}


// This will be reused
let inline checkClientIsVerified (flowData: 'a when 'a: (member ApiClient: ApiClient)) =
    match flowData.ApiClient.VerifiedAt.HasValue with
    | false -> Error (OAuthModels.ProtocolError.access_denied, "Client is not verified")
    | _ -> Ok flowData



// This will be reused
let inline checkClientIsActive (flowData: 'a when 'a: (member ApiClient: ApiClient)) =
    match flowData.ApiClient.IsActive with
    | false -> Error (OAuthModels.ProtocolError.access_denied, "Client is not active")
    | _ -> Ok flowData



let inline private checkUnfinishedRequestsCount flowData =
    match flowData.UnfinishedRequestsCount >= flowData.WorkflowData.AppLimits.UnfinishedOAuthRequestLimitPerHour with
    | true -> Error (OAuthModels.ProtocolError.access_denied, "Too many pending requests")
    | _ -> Ok flowData



let inline private checkCodeChallengeMethod flowData =
    match flowData.RequestModel.code_challenge_method with
    | None -> Ok flowData
    | Some value when value.ToUpper() = "S256" || value.ToLower() = "plain" -> Ok flowData
    | _ -> Error (OAuthModels.ProtocolError.invalid_request, "Invalid 'code_challenge_method', the allowed values are 'S256' and 'plain'")



let inline private checkResponseType flowData  =
    match flowData.RequestModel.response_type with
    | value when value.ToLower() = "code" -> Ok flowData
    | _ -> Error (OAuthModels.ProtocolError.unsupported_response_type, "Unsupported 'response_type', the allowed value is 'code'")



// This will be reused
let inline checkOAuthResource
    (flowData: 'a
        when 'a: (member ApiClient: ApiClient)
        and 'a: (member RequestModel: 'b 
            when 'b: (member resource: string list option))) =

    match flowData.RequestModel.resource with

    // If no resource is specified, and if this is not an 'openid' request
    // then check if client has access to at least one active resource
    | None | Some [] ->
        match
            flowData.ApiClient.ApiClientResources
            |> Seq.tryFind (fun clientResource -> clientResource.ApiResource.IsActive)
        with
        | Some _ -> Ok flowData
        | None -> Error (OAuthModels.ProtocolError.access_denied, "Client does not have access to any resource")

    // All of the specified resources must be available for this client and must be active
    | Some list ->
        match
            list
            |> List.tryFind (fun resource ->
                flowData.ApiClient.ApiClientResources
                |> Seq.tryFind (fun clientResource -> clientResource.ApiResourceId = resource && clientResource.ApiResource.IsActive)
                |> Option.isNone
            )
        with
        | Some invalidResource -> Error (OAuthModels.ProtocolError.invalid_target, $"The specified resource '{invalidResource}' is either invalid, inactive or unaccessible for this client")
        | None -> Ok flowData


    
let inline private checkResource flowData  =
    match flowData.RequestType with

    // If this is a 'openid' request, ignore resource parameter
    | AuthorizeRequestType.OpenId -> Ok flowData
    
    | _ -> checkOAuthResource flowData



// This will be reused
// Any specified scope must be available for all of the requested resources
let inline checkScopes
    (flowData: 'a
        when 'a: (member ApiClient: ApiClient)
        and 'a: (member RequestModel: 'b 
            when 'b: (member scope: string option)
            and 'b: (member resource: string list option))) =
    
    match flowData.RequestModel.scope with
    | None -> Ok flowData
    | Some value ->
        let resourcesWithScopes =
            match flowData.RequestModel.resource with
            | None ->
                flowData.ApiClient.ApiClientResources
                |> Seq.tryFind (fun clientResource ->
                    clientResource.ApiResource.IsActive
                    && Option.isSome <| Option.ofObj clientResource.AllowedScopes
                )
                |> Option.map (fun clientResource -> (clientResource.ApiResourceId, clientResource.AllowedScopes))
                |> Option.map (Array.create 1)
                |> Option.defaultValue (Array.empty)
            | Some list ->
                flowData.ApiClient.ApiClientResources
                |> Seq.filter (fun clientResource -> list |> List.contains clientResource.ApiResourceId)
                |> Seq.filter (fun clientResource ->
                    clientResource.ApiResource.IsActive
                    && Option.isSome <| Option.ofObj clientResource.AllowedScopes
                )
                |> Seq.map (fun clientResource -> (clientResource.ApiResourceId, clientResource.AllowedScopes))
                |> Seq.toArray

        value.Split ' '
        |> Array.except (Array.append openidScopes [| rhinoIdentityScope |])
        |> Array.tryFind (fun scope ->
            resourcesWithScopes
            |> Array.tryFind (fun (_, allowedScopes) -> not <| allowedScopes.Contains scope)
            |> Option.isSome
        )
        |> Option.map (fun invalidScope -> Error (OAuthModels.ProtocolError.invalid_scope, $"The scope '{invalidScope}' is either invalid or unaccessible for specified resources"))
        |> Option.defaultValue (Ok flowData)



let inline private createAuthorizeRequest flowData =

    let requestId = KeyGenerator.getString32To64Chars()

    let scopes =
        match flowData.RequestModel.scope with
        | Some value -> ResizeArray(value.Split ' ')
        | None -> ResizeArray()

    let resources =
        flowData.RequestModel.resource
        |> Option.map (fun list -> list |> List.map (fun id -> flowData.ApiClient.ApiClientResources |> Seq.find (fun cr -> cr.ApiResourceId = id)))
        |> Option.map (fun list -> list |> List.map (fun resource -> AuthorizeRequestApiResource(AuthorizeRequestId = requestId, ApiResourceId = resource.ApiResourceId)))
        |> Option.map ResizeArray
        |> Option.defaultValue (ResizeArray())
    
    let authorizeRequest = AuthorizeRequest(
        Id = requestId,
        RequestType = flowData.RequestType,
        CodeChallenge = flowData.RequestModel.code_challenge,
        VerifierMethod = 
            (match flowData.RequestModel.code_challenge_method with
            | Some method when method.ToUpper() = "S256" -> VerifierMethod.S256
            | _ -> VerifierMethod.Plain),
        Scopes = scopes,
        State = (flowData.RequestModel.state |> Option.toObj),
        Nonce = (flowData.RequestModel.nonce |> Option.toObj),
        ConsentedAt = (if flowData.ApiClient.ShowConsent then System.Nullable() else System.Nullable System.DateTimeOffset.UtcNow),
        LoginId = flowData.WorkflowData.User.GetRequiredSessionId(),
        UserId = flowData.WorkflowData.User.GetRequiredId(),
        ApiClientId = flowData.ApiClient.Id,
        AuthorizeRequestApiResources = resources
    )

    flowData.WorkflowData.DbCommands.CreateRow authorizeRequest
    |> TaskResult.map (fun _ -> authorizeRequest)
    |> TaskResult.mapError (function
        | ConcurrentDataAccess -> (OAuthModels.ProtocolError.temporarily_unavailable, "Concurrent requests are not supported")
        | _ -> (OAuthModels.ProtocolError.server_error, "Failed to process the request due to internal error")
    )



let inline private getFinalResponse (authorizeRequest: AuthorizeRequest) =
    match (authorizeRequest.ConsentedAt.HasValue, authorizeRequest.State |> Option.ofObj) with
    | (false, _) -> ShowConsent authorizeRequest.Id
    | (_, Some state) -> RedirectWithState (authorizeRequest.Id, state)
    | _ -> Redirect authorizeRequest.Id



let private detectRequestType (requestModel: OAuthModels.AuthorizeRequest) =
    match (requestModel.resource, requestModel.scope) with
    | (_, Some scope) when not (scope.Contains "openid") -> AuthorizeRequestType.OAuth
    
    | (Some resourceList, Some scope) when
        resourceList.Length > 0
        || scope.Contains rhinoIdentityScope -> AuthorizeRequestType.OpenId_OAuth
    
    | _ -> AuthorizeRequestType.OpenId



let entry wd requestModel apiClient unfinishedRequestsCount =
    {
        WorkflowData = wd
        RequestModel = requestModel
        RequestType = detectRequestType requestModel
        ApiClient = apiClient
        UnfinishedRequestsCount = unfinishedRequestsCount
    }
    |> checkClientIsVerified
    |> Result.bind checkClientIsActive
    |> Result.bind checkUnfinishedRequestsCount
    |> Result.bind checkCodeChallengeMethod
    |> Result.bind checkResponseType
    |> Result.bind checkResource
    |> Result.bind checkScopes
    |> TaskResult.ofResult
    |> TaskResult.bind createAuthorizeRequest
    |> TaskResult.map getFinalResponse

