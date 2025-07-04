module WorkflowSharedValidations

open FsToolkit.ErrorHandling
open RhinoAuth.Database
open Settings

open Dependencies.IpInfo
open WorkflowBuilder


// Prefetch validations

let inline checkPhoneNumber phoneNumberValidator (model: 'a when 'a: (member PhoneCode: int) and 'a: (member PhoneNumber: string)) =
    match phoneNumberValidator $"+{model.PhoneCode} {model.PhoneNumber}" with
    | false -> someError <| FormValidationError "Invalid or unsupported phone number"
    | _ -> None


let inline checkCaptcha captchaValidator ipAddress (model: 'a when 'a: (member Captcha: string)) =
    captchaValidator ipAddress model.Captcha
    |> Task.map (function
        | Ok isValid when isValid -> None
        | Ok _ -> someError <| FormValidationError "Invalid captcha"
        | Error msg -> someError <| ExternalServiceError msg
    )


let inline checkUnsafeNetwork operation appLimits ipCountry _ =
    match (appLimits.AllowUnsafeNetworks, ipCountry.IsUnsafeNetwork) with
    | (false, true) -> someError <| BusinessRuleError $"{operation} from unsafe networks (proxy, VPN) is not allowed"
    | _ -> None



// Business rules

let inline checkPhoneCountryExists _ (data: 'a when 'a: (member PhoneCountry: Country option)) =
    match data.PhoneCountry with
    | None -> someError <| BusinessRuleError "Invalid or unsupported country"
    | _ -> None


let inline checkPhoneNumberIsAvailable _ (data: 'a when 'a: (member PhoneNumberTaken: bool)) =
    match data.PhoneNumberTaken with
    | true -> someError <| BusinessRuleError "This phone number is not available"
    | _ -> None


let inline checkEmailIsAvailable _ (data: 'a when 'a: (member EmailTaken: bool)) =
    match data.EmailTaken with
    | true -> someError <| BusinessRuleError "This email is not available"
    | _ -> None


let inline checkSentCodesLimit codeLimit _ (user: User) =
    match
        user.OneTimeCodes
        |> Seq.filter (fun c -> not c.IsUsed)
        |> Seq.length
    with
    | n when n >= codeLimit -> someError <| BusinessRuleError "Maximum sent code limit has reached, try again later"
    | _ -> None


let inline checkLastUsedCode reason verb field allowedChangePerDay _ (user: User) =
    match
        user.OneTimeCodes
        |> Seq.filter (fun c -> c.Reason = reason)
        |> Seq.filter (fun c -> c.IsUsed)
        |> Seq.filter (fun c -> c.CreatedAt.AddDays 1 > System.DateTimeOffset.UtcNow)
        |> Seq.length
    with
    | n when n >= allowedChangePerDay -> someError <| BusinessRuleError $"You can request to {verb} your {field} {allowedChangePerDay} time(s) per day"
    | _ -> None


let inline checkOneTimeCodeFailedAttempts maxFailAttempt _ (otc: OneTimeCode) =
    match otc.FailedAttempts with
    | n when n >= maxFailAttempt -> someError <| BusinessRuleError "Maximum failed attempt has reached, please start again"
    | _ -> None


let inline checkOneTimeCodeExpiration expiresInMinute _ (otc: OneTimeCode) =
    match otc.CreatedAt with
    | date when date.AddMinutes expiresInMinute <= System.DateTimeOffset.UtcNow -> someError <| BusinessRuleError "This code has expired, please start again"
    | _ -> None


let inline checkOneTimeCodeIsUsed _ (otc: OneTimeCode) =
    match otc.IsUsed with
    | true -> someError <| FormValidationError "Invalid code"
    | _ -> None


let inline checkOneTimeCodeUserId userId _ (otc: OneTimeCode) =
    match otc.UserId = userId with
    | false -> someError <| FormValidationError "Invalid code"
    | _ -> None