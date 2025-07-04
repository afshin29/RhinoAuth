module Workflows.SignupVerificationWorkflows

open FsToolkit.ErrorHandling
open RhinoAuth.Database
open Utilities

open Settings
open FormModels
open Repository
open WorkflowBuilder


let inline private checkSignupVerificationStepForEmail _ (signupRequest: SignupRequest) =
    match signupRequest.SmsVerificationCode with
    | NonNull _ -> someError <| FormValidationError "Email has already been verified"
    | _ -> None


let inline private checkSignupVerificationStepForSms _ (signupRequest: SignupRequest) =
    match signupRequest.SmsVerificationCode with
    | Null -> someError <| BusinessRuleError "You must validate your email first"
    | _ -> None


let inline private checkVerificationExpiration _ (signupRequest: SignupRequest) =
    match signupRequest.ExpiresAt with
    | date when date < System.DateTimeOffset.UtcNow -> someError <| BusinessRuleError "This signup attempt has expired, please start again"
    | _ -> None


let inline private checkVerificationFailedAttempt maxFailAttempt _ (dbData: SignupRequest) =
    match dbData.FailedAttempts with
    | n when n >= maxFailAttempt -> someError <| BusinessRuleError "Maximum failed attempt has reached, please start again"
    | _ -> None


let inline private processEmailVerification wd (formModel: SignupVerificationFM) (signupRequest: SignupRequest) =

    let dbCommand () = wd.DbCommands.UpdateRow () |> mapDefaultDbError

    if formModel.Code = signupRequest.EmailVerificationCode then

        signupRequest.SmsVerificationCode <- KeyGenerator.getNumeric6Digits()
        signupRequest.FailedAttempts <- 0
        signupRequest.ExpiresAt <- System.DateTimeOffset.UtcNow.AddMinutes wd.AppLimits.CodeExpirationInMinute

        wd.DependencyProvider.SmsSender signupRequest.SmsVerificationCode
        |> mapDependencyError
        |> TaskResult.bind (fun _ -> dbCommand ())
        |> TaskResult.map (fun _ -> signupRequest.Id)

    else
        signupRequest.FailedAttempts <- signupRequest.FailedAttempts + 1
        
        dbCommand ()
        |> TaskResult.bind (fun _ -> Task.singleton (Error <| FormValidationError "Invalid code"))


let inline private processSmsVerification wd (formModel: SignupVerificationFM) (signupRequest: SignupRequest) =
    if formModel.Code = signupRequest.SmsVerificationCode then

        let newUser = User(
            Username = signupRequest.Username,
            PasswordHash = signupRequest.PasswordHash,
            CountryPhoneCode = signupRequest.CountryPhoneCode,
            PhoneNumber = signupRequest.PhoneNumber,
            Email = signupRequest.Email,
            FirstName = signupRequest.FirstName,
            LastName = signupRequest.LastName,
            CountryCode = signupRequest.CountryCode
        )

        wd.DbCommands.CreateUser signupRequest newUser
        |> TaskResult.mapError (function
            | DuplicateIndex column when column = nameof newUser.Username -> BusinessRuleError $"""The username "{signupRequest.Username}" is not available anymore, please start again"""
            | DuplicateIndex column when column = nameof newUser.Email -> BusinessRuleError $"""The email "{signupRequest.Email}" is not available anymore, please start again"""
            | DuplicateIndex column when column = nameof newUser.PhoneNumber -> BusinessRuleError $"""The phone number "+{signupRequest.CountryPhoneCode}{signupRequest.PhoneNumber}" is not available anymore, please start again"""
            | other -> dbErrorMapper other
        )
        |> TaskResult.map (fun _ -> "")

    else
        signupRequest.FailedAttempts <- signupRequest.FailedAttempts + 1
        
        wd.DbCommands.UpdateRow ()
        |> mapDefaultDbError
        |> TaskResult.bind (fun _ -> Task.singleton (Error <| FormValidationError "Invalid code"))
        

let emailVerificationWorkflow wd formModel =

    let businessRules = seq {
        checkSignupVerificationStepForEmail
        checkVerificationExpiration
        checkVerificationFailedAttempt wd.AppLimits.CodeRetryLimit
    }

    let dbQuery () =
        wd.DbQueries.GetSignupRequest formModel.SignupId
        |> mapDbOptionToResult

    build [] [] dbQuery businessRules processEmailVerification wd formModel



let phoneVerificationWorkflow wd formModel =

    let businessRules = seq {
        checkSignupVerificationStepForSms
        checkVerificationExpiration
        checkVerificationFailedAttempt wd.AppLimits.CodeRetryLimit
    }

    let dbQuery () =
        wd.DbQueries.GetSignupRequest formModel.SignupId
        |> mapDbOptionToResult

    build [] [] dbQuery businessRules processSmsVerification wd formModel