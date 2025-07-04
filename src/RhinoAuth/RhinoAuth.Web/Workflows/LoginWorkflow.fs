module Workflows.LoginWorkflow

open FsToolkit.ErrorHandling
open Microsoft.IdentityModel.JsonWebTokens
open RhinoAuth.Database
open System.Security.Claims
open Utilities

open Dependencies.IpInfo
open Settings
open FormModels
open Repository
open WorkflowBuilder


let inline private checkUserLockout _ (user: User) =
    match Option.ofNullable user.LockoutEndsAt with
    | Some date when date > System.DateTimeOffset.UtcNow -> someError <| BusinessRuleError "Account is locked, try again later"
    | _ -> None


let inline private checkUserBlocked _ (user: User) =
    match Option.ofNullable user.BlockedAt with
    | Some _ -> someError <| BusinessRuleError "Account is blocked"
    | _ -> None


let inline private checkCountryLoginIsAllowed currentCountry _ (user: User) =
    match (user.Country.AllowIpLogin, user.Country.AllowPhoneNumberLogin) with
    | (_, false) -> someError <| BusinessRuleError "Login for this country is not allowed"
    | (false, _) when user.CountryCode = currentCountry  -> someError <| BusinessRuleError "Login from your region is not allowed"
    | _ -> None


let inline private processLogin wd formModel (user: User) =

    let login = Login(
        Id = KeyGenerator.getString32To64Chars(),
        IpAddress = wd.IpAddress,
        IsPersistent = formModel.RemeberMe.IsSome,
        UserAgent = (wd.HeaderProvider Microsoft.Net.Http.Headers.HeaderNames.UserAgent |> Option.toObj),
        UserId = user.Id
    )
    
    let loginResult =
        if PasswordHasher.isValidHashedPassword user.PasswordHash formModel.Password then
            user.FailedLoginAttempts <- 0
            login.Successful <- true

            Ok ([
                Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString())
                Claim(JwtRegisteredClaimNames.UniqueName, user.Username)
                Claim(JwtRegisteredClaimNames.Sid, login.Id)
            ], formModel.RemeberMe.IsSome)
        
        else
            user.FailedLoginAttempts <- user.FailedLoginAttempts + 1
            
            if user.FailedLoginAttempts >= wd.AppLimits.WrongPasswordLimit then
                user.FailedLoginAttempts <- 0
                user.LockoutEndsAt <- System.DateTimeOffset.UtcNow.AddMinutes wd.AppLimits.AccountLockoutInMinute

            Error <| FormValidationError "Invalid username or password"
    
    wd.DbCommands.CreateRow login
    |> mapDefaultDbError
    |> TaskResult.bind (fun _ -> Task.singleton loginResult)
    

let entry ipCountry wd (formModel: LoginFM) =

    let businessRules = seq {
        checkUserLockout
        checkUserBlocked
        checkCountryLoginIsAllowed ipCountry.CountryCode
    }

    let dbQuery () =
        wd.DbQueries.GetUserIncludingCountry formModel.Username
        |> mapDbOptionToResult
        |> TaskResult.mapError (function
            | ResourceNotFound -> FormValidationError "Invalid username or password"
            | err -> err
        )

    build [] [] dbQuery businessRules processLogin wd formModel
    
