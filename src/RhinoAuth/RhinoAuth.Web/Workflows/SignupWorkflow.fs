module Workflows.SignupWorkflow

open FsToolkit.ErrorHandling
open RhinoAuth.Database
open Utilities

open Dependencies.EmailSender
open Dependencies.IpInfo
open Settings
open FormModels
open Repository
open WorkflowBuilder
open WorkflowSharedValidations


let inline private checkResgistrationAttemptLimit registrationLimit _ dbData =
    match dbData.TotalRequestCount with
    | n when n > registrationLimit -> someError <| BusinessRuleError "Registration is temporarily closed, please try again later"
    | _ -> None


let inline private checkIpAttemptLimit ipLimit _ dbData =
    match dbData.IpRequestCount with
    | n when n > ipLimit -> someError <| BusinessRuleError "Too many attempts, please try again later"
    | _ -> None


let inline private checkPhoneNumberAttemptLimit phoneNumberLimit _ dbData =
    match dbData.PhoneNumberRequestCount with
    | n when n > phoneNumberLimit -> someError <| BusinessRuleError "Too many attempts for this phone number, please try again later"
    | _ -> None


let inline private checkIpCountryExists _ dbData =
    match dbData.IpCountry with
    | None -> someError <| BusinessRuleError "Registrations from unknown regions are not allowed"
    | _ -> None


let inline private checkCountryRegistrationIsAllowed _ (dbData: SignupDbData) =
    match (dbData.PhoneCountry, dbData.IpCountry) with
    | (Some phoneCountry, _) when not phoneCountry.AllowPhoneNumberResgistration -> someError <| BusinessRuleError "Registration for this country is not allowed"
    | (_, Some ipCountry) when not ipCountry.AllowIpResgistration -> someError <| BusinessRuleError "Registration from your region is not allowed"
    | _ -> None


let inline private checkUsernameIsAvailable _ dbData =
    match dbData.UsernameTaken with
    | true -> someError <| BusinessRuleError "This username is not available"
    | _ -> None


let inline private proccessSignup wd (formModel: SignupFM) (dbData: SignupDbData) =

    let signupRequest = SignupRequest(
        Id = KeyGenerator.getString32To64Chars(),
        IpAddress = wd.IpAddress,
        UserAgent = (wd.HeaderProvider Microsoft.Net.Http.Headers.HeaderNames.UserAgent |> Option.toObj),
        CountryCode = dbData.PhoneCountry.Value.Code,
        CountryPhoneCode = formModel.PhoneCode,
        PhoneNumber = formModel.PhoneNumber.TrimStart('0'),
        Email = formModel.Email.ToLower(),
        Username = formModel.Username.ToLower(),
        FirstName = formModel.FirstName,
        LastName = formModel.LastName,
        PasswordHash = PasswordHasher.hashPassword formModel.Password,
        EmailVerificationCode = KeyGenerator.getNumeric8Digits(),
        ExpiresAt = System.DateTimeOffset.UtcNow.AddMinutes wd.AppLimits.CodeExpirationInMinute
    )

    wd.DependencyProvider.EmailSender Verification signupRequest.EmailVerificationCode
    |> mapDependencyError
    |> TaskResult.bind (fun _ -> wd.DbCommands.CreateRow signupRequest |> mapDefaultDbError)
    |> TaskResult.map (fun _ -> signupRequest.Id)


let entry ipCountry wd formModel =

    let prefetchValidations = seq {
        toTask <| checkPhoneNumber Utilities.PhoneNumberValidator.isPhoneNumberValid
        checkCaptcha wd.DependencyProvider.CaptchaValidator wd.IpAddress
        toTask <| checkUnsafeNetwork "Registration" wd.AppLimits ipCountry
    }

    let businessRules = seq {
        checkResgistrationAttemptLimit wd.AppLimits.RegistrationAttemptLimit
        checkIpAttemptLimit wd.AppLimits.IpAttemptLimit
        checkPhoneNumberAttemptLimit wd.AppLimits.PhoneNumberAttemptLimit
        checkPhoneCountryExists
        checkIpCountryExists
        checkCountryRegistrationIsAllowed
        checkPhoneNumberIsAvailable
        checkUsernameIsAvailable
        checkEmailIsAvailable
    }

    let dbQuery () =
        wd.DbQueries.GetSignupData wd.IpAddress formModel ipCountry.CountryCode
        |> TaskResult.ofTask

    build FormValidations.signupFormValidation prefetchValidations dbQuery businessRules proccessSignup wd formModel
