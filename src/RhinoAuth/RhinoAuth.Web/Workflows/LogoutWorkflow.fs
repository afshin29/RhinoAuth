module LogoutWorkflow

open FsToolkit.ErrorHandling
open RhinoAuth.Database
open Settings
open System
open Utilities

open Repository
open WorkflowBuilder


let inline private logoutForUnauthorizedUser wd (formModel: OAuthModels.LogoutFM) = task {
    let! externalLogin = wd.DbQueries.GetExternalLogin formModel.id_token_hint.Value
    
    return
        match externalLogin with
        | None -> "/"
        | Some session ->
            if Option.isSome <| Option.ofObj session.AccessToken then
                wd.DependencyProvider.DistributedCache.StringSet session.AccessToken "1" (TimeSpan.FromHours 1)
            
            match formModel.state with
            | None -> session.ApiClient.LogoutCallbackUri
            | Some state -> $"{session.ApiClient.LogoutCallbackUri}?state={state}"
}


let inline private logoutForAuthorizedUser wd (formModel: OAuthModels.LogoutFM) = task {
    let! login = wd.DbQueries.GetLogin <| wd.User.GetRequiredSessionId()

    match login with
    | None -> return! logoutForUnauthorizedUser wd formModel
    
    | Some session ->

        let externalLogin =
            session.ExternalLogins
            |> Seq.tryFind (fun el -> el.IdToken = (formModel.id_token_hint |> Option.defaultValue ""))

        session.LogoutIpAddress <- wd.IpAddress
        session.EndedAt <- Nullable DateTimeOffset.UtcNow
        if externalLogin.IsSome then
            session.EndedByExternalLoginId = externalLogin.Value.Id |> ignore

        session.ExternalLogins
        |> Seq.filter (fun el -> not <| isNull el.AccessToken)
        |> Seq.iter (fun el -> wd.DependencyProvider.DistributedCache.StringSet el.AccessToken "1" (TimeSpan.FromHours 1))

        session.ExternalLogins
        |> Seq.except (if externalLogin.IsSome then [externalLogin.Value] else [])
        |> Seq.map _.Id
        |> Seq.iter Services.BackchannelClientCaller.ExternalLoginIds.Add

        return
           match (externalLogin, formModel.state) with
           | (None, _) -> "/"
           | (Some extSession, Some state) -> $"{extSession.ApiClient.LogoutCallbackUri}?state={state}"
           | (Some extSession, None) -> extSession.ApiClient.LogoutCallbackUri
}


let entry wd (formModel: OAuthModels.LogoutFM) =
    match wd.User.IsLoggedIn() with
    | false when formModel.id_token_hint.IsNone -> "/" |> Task.singleton
    | false -> logoutForUnauthorizedUser wd formModel
    | true -> logoutForAuthorizedUser wd formModel