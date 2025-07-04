module Workflows.ProfileWorkflows

open FsToolkit.ErrorHandling
open RhinoAuth.Database
open Utilities
open Settings

open Dependencies.EmailSender
open Dependencies.IpInfo
open FormModels
open Repository
open WorkflowBuilder
open WorkflowSharedValidations


let inline private checkImageType (formModel: ChangeAvatarFM) =
    use fileStream = new System.IO.MemoryStream()
    formModel.Avatar.CopyTo fileStream
    match FileValidator.isValidJpegImage (fileStream.ToArray()) with
    | false -> someError <| BusinessRuleError "Invalid or unsupported image type"
    | _ -> None


let inline private checkProfileUpdateLimit changeLimit _ (user: User) =
    match
        user.ProfileUpdateHistory
        |> Seq.filter (fun c -> c.CreatedAt.AddDays 1 > System.DateTimeOffset.UtcNow)
        |> Seq.length
    with
    | n when n >= changeLimit -> someError <| BusinessRuleError $"You can update your profile {changeLimit} time(s) per day"
    | _ -> None


let inline private removeUserAvatarFile wd (user: User) =
    user.Avatar
    |> Option.ofObj
    |> Option.map (fun fileName -> wd.DependencyProvider.FileManager.DeleteProfilePic fileName)
    |> Option.defaultValue (TaskResult.ok ())


let inline private processChangeAvatar wd (formModel: ChangeAvatarFM) (user: User) =

    let newFileName = KeyGenerator.getString32To64Chars()
    use fileContent = new System.IO.MemoryStream()
    formModel.Avatar.CopyTo fileContent
    
    removeUserAvatarFile wd user
    |> TaskResult.bind (fun _ -> wd.DependencyProvider.FileManager.SaveProfilePic newFileName (fileContent.ToArray()))
    |> mapDependencyError
    |> TaskResult.map (fun _ ->
        user.AddProfileHistoryRow()
        user.Avatar <- newFileName
    )
    |> TaskResult.bind (fun _ -> wd.DbCommands.UpdateRow () |> mapDefaultDbError)



let changeAvatarWorkflow wd formModel =

    let prefetchValidations = seq {
        toTask <| checkImageType
    }

    let businessRules = seq {
        checkProfileUpdateLimit wd.AppLimits.ChangeProfileLimitPerDay
    }

    let dbQuery () =
        wd.DbQueries.GetUser (wd.User.GetRequiredId())
        |> mapDbOptionToResult

    build FormValidations.changeAvatarFormValidation prefetchValidations dbQuery businessRules processChangeAvatar wd formModel



let inline private processRemoveAvatar wd _ (user: User) =

    removeUserAvatarFile wd user
    |> mapDependencyError
    |> TaskResult.map (fun _ ->
        user.AddProfileHistoryRow()
        user.Avatar <- null
    )
    |> TaskResult.bind (fun _ -> wd.DbCommands.UpdateRow () |> mapDefaultDbError)


let removeAvatarWorkflow wd _ =

    let dbQuery () =
        wd.DbQueries.GetUser (wd.User.GetRequiredId())
        |> mapDbOptionToResult

    build [] [] dbQuery [] processRemoveAvatar wd None



let inline private processChangeName wd (formModel: ChangeNameFM) (user: User) =

    user.AddProfileHistoryRow()
    user.FirstName <- formModel.FirstName
    user.LastName <- formModel.LastName

    wd.DbCommands.UpdateRow ()
    |> mapDefaultDbError


let changeNameWorkflow wd formModel =

    let businessRules = seq {
        checkProfileUpdateLimit wd.AppLimits.ChangeProfileLimitPerDay
    }

    let dbQuery () =
        wd.DbQueries.GetUser (wd.User.GetRequiredId())
        |> mapDbOptionToResult

    build FormValidations.changeNameFormValidations [] dbQuery businessRules processChangeName wd formModel


    
let inline private checkLastPhoneNumberChange allowedChangePerDay fm (dbData: ChangePhoneNumberDbData) = checkLastUsedCode OneTimeCodeReason.PhoneNumber "change" "phone number" allowedChangePerDay fm dbData.User


let inline private processRequestPhoneNumberChange wd (formModel: RequestPhoneNumberChangeFM) (dbData: ChangePhoneNumberDbData) =

    let otc = OneTimeCode(
        Id = KeyGenerator.getString32To64Chars(),
        Code = KeyGenerator.getNumeric6Digits(),
        Reason = OneTimeCodeReason.PhoneNumber,
        IpAddress = wd.IpAddress,
        UserAgent = (wd.HeaderProvider Microsoft.Net.Http.Headers.HeaderNames.UserAgent |> Option.toObj),
        UserId = wd.User.GetRequiredId()
    )

    dbData.User.OneTimeCodes.Add otc
    dbData.User.UnverifiedCountryCode <- dbData.PhoneCountry.Value.Code
    dbData.User.UnverifiedCountryPhoneCode <- formModel.PhoneCode
    dbData.User.UnverifiedPhoneNumber <- formModel.PhoneNumber.TrimStart('0')

    wd.DependencyProvider.SmsSender otc.Code
    |> mapDependencyError
    |> TaskResult.bind (fun _ -> wd.DbCommands.UpdateRow () |> mapDefaultDbError)
    |> TaskResult.map (fun _ -> otc.Id)


let requestPhoneNumberChangeWorkflow wd formModel =

    let prefetchValidations = seq {
        toTask <| checkPhoneNumber Utilities.PhoneNumberValidator.isPhoneNumberValid
    }

    let businessRules = seq {
        checkPhoneCountryExists
        checkPhoneNumberIsAvailable

        // We need to extract the inner User to use the shared rule
        (fun fm (dbData: ChangePhoneNumberDbData) -> checkSentCodesLimit wd.AppLimits.UnusedCodesLimit fm dbData.User)

        checkLastPhoneNumberChange wd.AppLimits.ChangePhoneNumberLimitPerDay
    }

    let dbQuery () =
        wd.DbQueries.GetChangePhoneNumberData (wd.User.GetRequiredId()) formModel.PhoneCode formModel.PhoneNumber
        |> TaskResult.ofTask

    build FormValidations.changePhoneNumberFormValidations prefetchValidations dbQuery businessRules processRequestPhoneNumberChange wd formModel



let inline private processVerifyPhoneNumberCode wd (formModel: VerifyCodeFM) (otc: OneTimeCode) =

    let result =
        if formModel.Code = otc.Code then
            otc.User.CountryCode <- otc.User.UnverifiedCountryCode
            otc.User.CountryPhoneCode <- otc.User.UnverifiedCountryPhoneCode.Value
            otc.User.PhoneNumber <- otc.User.UnverifiedPhoneNumber
            
            otc.User.UnverifiedCountryCode <- null
            otc.User.UnverifiedCountryPhoneCode <- System.Nullable<int>()
            otc.User.UnverifiedPhoneNumber <- null
            
            otc.IsUsed <- true
            Ok ()
        
        else
            otc.FailedAttempts <- otc.FailedAttempts + 1
            Error <| FormValidationError "Invalid code"

    wd.DbCommands.UpdateRow ()
    |> TaskResult.mapError (function
        | DuplicateIndex column when column = nameof otc.User.PhoneNumber -> BusinessRuleError $"""The phone number "+{otc.User.UnverifiedCountryPhoneCode}{otc.User.UnverifiedPhoneNumber}" is not available anymore, please start again"""
        | other -> dbErrorMapper other
    )
    |> TaskResult.bind (fun _ -> Task.singleton result)


let verifyPhoneNumberCode wd formModel =

    let businessRules = seq {
        checkOneTimeCodeUserId <| wd.User.GetRequiredId()
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

    build [] [] dbQuery businessRules processVerifyPhoneNumberCode wd formModel



let inline private checkLastEmailChange allowedChangePerDay fm (dbData: ChangeEmailDbData) = checkLastUsedCode OneTimeCodeReason.PhoneNumber "change" "email" allowedChangePerDay fm dbData.User


let inline private processRequestEmailChange wd (formModel: RequestEmailChangeFM) (dbData: ChangeEmailDbData) =

    let otc = OneTimeCode(
        Id = KeyGenerator.getString32To64Chars(),
        Code = KeyGenerator.getNumeric6Digits(),
        Reason = OneTimeCodeReason.Email,
        IpAddress = wd.IpAddress,
        UserAgent = (wd.HeaderProvider Microsoft.Net.Http.Headers.HeaderNames.UserAgent |> Option.toObj),
        UserId = wd.User.GetRequiredId()
    )

    dbData.User.OneTimeCodes.Add otc
    dbData.User.UnverifiedEmail <- formModel.Email.ToLower()

    wd.DependencyProvider.EmailSender Verification otc.Code
    |> mapDependencyError
    |> TaskResult.bind (fun _ -> wd.DbCommands.UpdateRow () |> mapDefaultDbError)
    |> TaskResult.map (fun _ -> otc.Id)


let requestEmailChangeWorkflow wd formModel =

    let businessRules = seq {
        // We need to extract the inner User to use the shared rule
        (fun fm (dbData: ChangeEmailDbData) -> checkSentCodesLimit wd.AppLimits.UnusedCodesLimit fm dbData.User)

        checkLastEmailChange wd.AppLimits.ChangeEmailLimitPerDay
    }

    let dbQuery () =
        wd.DbQueries.GetChangeEmailData (wd.User.GetRequiredId()) formModel.Email
        |> TaskResult.ofTask

    build FormValidations.changeEmailFormValidations [] dbQuery businessRules processRequestEmailChange wd formModel



let inline private processVerifyEmailCode wd (formModel: VerifyCodeFM) (otc: OneTimeCode) =

    let result =
        if formModel.Code = otc.Code then
            otc.User.Email <- otc.User.UnverifiedEmail
            otc.User.UnverifiedEmail <- null
            
            otc.IsUsed <- true
            Ok ()
        
        else
            otc.FailedAttempts <- otc.FailedAttempts + 1
            Error <| FormValidationError "Invalid code"

    wd.DbCommands.UpdateRow ()
    |> TaskResult.mapError (function
        | DuplicateIndex column when column = nameof otc.User.Email -> BusinessRuleError $"""The email "{otc.User.UnverifiedEmail}" is not available anymore, please start again"""
        | other -> dbErrorMapper other
    )
    |> TaskResult.bind (fun _ -> Task.singleton result)


let verifyEmailCode wd formModel =

    let businessRules = seq {
        checkOneTimeCodeUserId <| wd.User.GetRequiredId()
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

    build [] [] dbQuery businessRules processVerifyEmailCode wd formModel




// Here there is no brute-force protection, users can brute-force themselves!
let inline private checkCurrentPassword formModel (user: User) =
    match PasswordHasher.isValidHashedPassword user.PasswordHash formModel.CurrentPassword with
    | false -> someError <| FormValidationError "Invalid password"
    | _ -> None


let inline private checkPasswordsAreSame formModel _ =
    match formModel.CurrentPassword = formModel.Password with
    | true -> someError <| FormValidationError "Choose a different password than your current one"
    | _ -> None


let inline private processChangePassword wd (formModel: ChangePasswordFM) (user: User) =

    user.PasswordHash <- PasswordHasher.hashPassword formModel.Password

    wd.DbCommands.UpdateRow ()
    |> mapDefaultDbError


let changePasswordWorkflow wd formModel =

    let businessRules = seq {
        checkCurrentPassword
        checkPasswordsAreSame
    }

    let dbQuery () =
        wd.DbQueries.GetUser (wd.User.GetRequiredId())
        |> mapDbOptionToResult

    build FormValidations.changePasswordFormValidations [] dbQuery businessRules processChangePassword wd formModel