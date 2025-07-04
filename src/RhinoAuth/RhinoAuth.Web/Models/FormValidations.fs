module FormValidations

open Microsoft.AspNetCore.Http
open System.Text.RegularExpressions

open FormModels


[<Literal>]
let nameRegex = "^[a-zA-Z' ]+$"

[<Literal>]
let nameMinLength = 2

[<Literal>]
let nameMaxLength = 30

[<Literal>]
let phoneNumberRegex = "^\d+$"

[<Literal>]
let phoneNumberPlaceholder = "Digits only"

[<Literal>]
let phoneNumberMaxLength = 15

[<Literal>]
let emailRegex = "^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$"

[<Literal>]
let usernameRegex = "^[a-z](?!.*__)[a-z0-9_]*[a-z0-9]$"

[<Literal>]
let usernameMinLength = 6

[<Literal>]
let usernameMaxLength = 16

[<Literal>]
let passwordMinLength = 8

[<Literal>]
let passwordMaxLength = 60

[<Literal>]
let avatarMaxSize = 1024 * 1000 * 5 // 500 KiB


// Each field can be in more than one form model
// so we use Duck Typing


let inline private checkFirstNameRegex (model: 'a when 'a: (member FirstName: string)) =
    match Regex.IsMatch(model.FirstName, nameRegex) with
    | false -> Some "Invalid input for First Name"
    | _ -> None


let inline private checkFirstNameLength (model: 'a when 'a: (member FirstName: string)) =
    match model.FirstName.Length with
    | l when l < nameMinLength || l > nameMaxLength -> Some $"First Name length must be between {nameMinLength} and {nameMaxLength} characters"
    | _ -> None


let inline private checkLastNameRegex (model: 'a when 'a: (member LastName: string)) =
    match Regex.IsMatch(model.LastName, nameRegex) with
    | false -> Some "Invalid input for Last Name"
    | _ -> None


let inline private checkLastNameLength (model: 'a when 'a: (member LastName: string)) =
    match model.LastName.Length with
    | l when l < nameMinLength || l > nameMaxLength -> Some $"Last Name length must be between {nameMinLength} and {nameMaxLength} characters"
    | _ -> None


let inline private checkPhoneCode (model: 'a when 'a: (member PhoneCode: int)) =
    match model.PhoneCode with
    | v when v < 1 || v > 999 -> Some "Invalid phone code"
    | _ -> None


let inline private checkPhoneNumberLength (model: 'a when 'a: (member PhoneNumber: string)) =
    match model.PhoneNumber.Length with
    | l when l > phoneNumberMaxLength -> Some "Invalid or unsupported phone number"
    | _ -> None


let inline private checkPhoneNumberRegex (model: 'a when 'a: (member PhoneNumber: string)) =
    match Regex.IsMatch(model.PhoneNumber, phoneNumberRegex) with
    | false -> Some "Invalid or unsupported phone number"
    | _ -> None


let inline private checkEmailRegex (model: 'a when 'a: (member Email: string)) =
    match Regex.IsMatch(model.Email, emailRegex) with
    | false -> Some "Invalid or unsupported email"
    | _ -> None


let inline private checkUsernameLength (model: 'a when 'a: (member Username: string)) =
    match model.Username.Length with
    | l when l < usernameMinLength || l > usernameMaxLength -> Some $"Username length must be between {usernameMinLength} and {usernameMaxLength} characters"
    | _ -> None


let inline private checkUsernameRegex (model: 'a when 'a: (member Username: string)) =
    match Regex.IsMatch(model.Username, usernameRegex) with
    | false -> Some "Invalid input for Username"
    | _ -> None


let inline private checkPasswordLength (model: 'a when 'a: (member Password: string)) =
    match model.Password.Length with
    | l when l < passwordMinLength || l > passwordMaxLength -> Some $"Password length must be between {passwordMinLength} and {passwordMaxLength} characters"
    | _ -> None


let inline private checkAvatarSize (model: 'a when 'a: (member Avatar: IFormFile)) =
    match model.Avatar.Length with
    | size when size > avatarMaxSize -> Some $"Maximum allowed size is {avatarMaxSize / 1024} KiB"
    | _ -> None



let signupFormValidation : (SignupFM -> string option) seq = seq {
    checkFirstNameRegex
    checkFirstNameLength
    checkLastNameRegex
    checkLastNameLength
    checkPhoneCode
    checkPhoneNumberLength
    checkPhoneNumberRegex
    checkEmailRegex
    checkUsernameLength
    checkUsernameRegex
    checkPasswordLength
}


let resetPasswordFormValidation : (ResetPasswordFM -> string option) seq = seq {
    checkPasswordLength
}


let changeAvatarFormValidation : (ChangeAvatarFM -> string option) seq = seq {
    checkAvatarSize
}


let changeNameFormValidations : (ChangeNameFM -> string option) seq = seq {
    checkFirstNameRegex
    checkFirstNameLength
    checkLastNameRegex
    checkLastNameLength
}


let changePhoneNumberFormValidations : (RequestPhoneNumberChangeFM -> string option) seq = seq {
    checkPhoneCode
    checkPhoneNumberLength
    checkPhoneNumberRegex
}


let changeEmailFormValidations : (RequestEmailChangeFM -> string option) seq = seq {
    checkEmailRegex
}


let changePasswordFormValidations : (ChangePasswordFM -> string option) seq = seq {
    checkPasswordLength
}