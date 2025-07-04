module Workflows.ConsentWorkflows

open FsToolkit.ErrorHandling
open RhinoAuth.Database
open System

open Repository
open WorkflowBuilder


let inline private checkRequestIsConsented _ (authorizeRequest: AuthorizeRequest) =
    match authorizeRequest.ConsentedAt.HasValue with
    | true -> someError <| BusinessRuleError "This requested is already consented"
    | _ -> None


let inline private checkRequestIsFinished _ (authorizeRequest: AuthorizeRequest) =
    match authorizeRequest.CompletedAt.HasValue with
    | true -> someError <| BusinessRuleError "This requested is finished"
    | _ -> None


let inline private checkRequestIsExpired limitMinutes _ (authorizeRequest: AuthorizeRequest) =
    match authorizeRequest.CreatedAt.AddMinutes limitMinutes < DateTimeOffset.UtcNow with
    | true -> someError <| BusinessRuleError "This requested is epired"
    | _ -> None


let inline private proccessConsent isAccepted wd _ (authorizeRequest: AuthorizeRequest) =

    if isAccepted then
        authorizeRequest.ConsentedAt <- Nullable DateTimeOffset.UtcNow
    else
        authorizeRequest.CompletedAt <- Nullable DateTimeOffset.UtcNow

    wd.DbCommands.UpdateRow authorizeRequest
    |> mapDefaultDbError
    |> TaskResult.map (fun _ ->

        match (isAccepted, authorizeRequest.State |> Option.ofObj) with
        | (true, Some state) -> $"{authorizeRequest.ApiClient.LoginCallbackUri}?code={authorizeRequest.Id}&state={state}&iss={wd.AppBehavior.OAuthIssuer}"
        | (true, None) -> $"{authorizeRequest.ApiClient.LoginCallbackUri}?code={authorizeRequest.Id}&iss={wd.AppBehavior.OAuthIssuer}"
        | _ -> $"{authorizeRequest.ApiClient.LoginCallbackUri}?error=access_denied&error_description=The request was rejected by user"
    )


let private updateAuthorizeRequest isAccepted wd (formModel: OAuthModels.ConsentFM) =
    
    let oauthValidations = seq {
        checkRequestIsConsented
        checkRequestIsFinished
        checkRequestIsExpired OAuthModels.expirationInMinutes
    }

    let dbQuery () =
        wd.DbQueries.GetAuthorizeRequest formModel.Code (wd.User.GetRequiredId())
        |> mapDbOptionToResult

    let processor = proccessConsent isAccepted

    build [] [] dbQuery oauthValidations processor wd formModel



let acceptAuthorizeRequest = updateAuthorizeRequest true

let rejectAuthorizeRequest = updateAuthorizeRequest false