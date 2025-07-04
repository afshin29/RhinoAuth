module Workflows.PasswordRecoveryWorkflows

open FsToolkit.ErrorHandling
open RhinoAuth.Database
open Utilities

open Dependencies.EmailSender
open Settings
open FormModels
open Repository
open WorkflowBuilder
open WorkflowSharedValidations


let inline private checkLastResetPassword allowedChangePerDay = checkLastUsedCode OneTimeCodeReason.Password "reset" "password" allowedChangePerDay


let inline private processForgotPassword wd formModel (user: User) =

    let otc = OneTimeCode(
        Id = KeyGenerator.getString32To64Chars(),
        Code = KeyGenerator.getNumeric8Digits(),
        Reason = OneTimeCodeReason.Password,
        IpAddress = wd.IpAddress,
        UserAgent = (wd.HeaderProvider Microsoft.Net.Http.Headers.HeaderNames.UserAgent |> Option.toObj),
        UserId = user.Id
    )

    wd.DependencyProvider.EmailSender Verification otc.Code
    |> mapDependencyError
    |> TaskResult.bind (fun _ -> wd.DbCommands.CreateRow otc |> mapDefaultDbError)
    |> TaskResult.map (fun _ -> otc.Id)


let forgotPasswordWorkflow wd (formModel: ForgotPasswordFM) =

    let prefetchValidations = seq {
        checkCaptcha wd.DependencyProvider.CaptchaValidator wd.IpAddress
    }

    let businessRules = seq {
        checkSentCodesLimit wd.AppLimits.UnusedCodesLimit
        checkLastResetPassword wd.AppLimits.ResetPasswordLimitPerDay
    }

    let dbQuery () =
        wd.DbQueries.GetUserIncludingOneTimeCodes formModel.Username
        |> mapDbOptionToResult
        |> TaskResult.mapError (fun err ->
            match err with
            | ResourceNotFound -> FormValidationError "Invalid username"
            | _ -> err
        )
    
    build [] prefetchValidations dbQuery businessRules processForgotPassword wd formModel




let inline private checkCodeIsForResetPassword _ (otc: OneTimeCode) =
    match otc.Reason = OneTimeCodeReason.Password with
    | false -> someError <| FormValidationError "Invalid code"
    | _ -> None


let inline private processResetPassword wd (formModel: ResetPasswordFM) (otc: OneTimeCode) =

    let result =
        if formModel.Code = otc.Code then
            otc.User.PasswordHash <- PasswordHasher.hashPassword formModel.Password
            otc.IsUsed <- true
            Ok ()
        
        else
            otc.FailedAttempts <- otc.FailedAttempts + 1
            Error <| FormValidationError "Invalid code"

    wd.DbCommands.UpdateRow ()
    |> mapDefaultDbError
    |> TaskResult.bind (fun _ -> Task.singleton result)


let resetPasswordWorkflow wd (formModel: ResetPasswordFM) =

    let businessRules = seq {
        checkCodeIsForResetPassword
        checkOneTimeCodeIsUsed
        checkOneTimeCodeFailedAttempts wd.AppLimits.CodeRetryLimit
        checkOneTimeCodeExpiration wd.AppLimits.CodeExpirationInMinute
    }

    let dbQuery () =
        wd.DbQueries.GetOneTimeCode formModel.CodeId
        |> mapDbOptionToResult
        |> TaskResult.mapError (fun err ->
            match err with
            | ResourceNotFound -> FormValidationError "Invalid code"
            | _ -> err
        )

    build FormValidations.resetPasswordFormValidation [] dbQuery businessRules processResetPassword wd formModel