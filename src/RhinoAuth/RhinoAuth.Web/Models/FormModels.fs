module FormModels

open Microsoft.AspNetCore.Http


[<CLIMutable>]
type SignupFM = {
    FirstName: string
    LastName: string
    PhoneCode: int
    PhoneNumber: string
    Email: string
    Username: string
    Password: string
    Captcha: string
}


[<CLIMutable>]
type SignupVerificationFM = {
    SignupId: string
    Code: string
}


[<CLIMutable>]
type LoginFM = {
    Username: string
    Password: string
    RemeberMe: bool option
}


[<CLIMutable>]
type ForgotPasswordFM = {
    Username: string
    Captcha: string
}


[<CLIMutable>]
type ResetPasswordFM = {
    CodeId: string
    Code: string
    Password: string
}


type ChangeAvatarFM = {
    Avatar: IFormFile
}


[<CLIMutable>]
type ChangeNameFM = {
    FirstName: string
    LastName: string
}


[<CLIMutable>]
type RequestPhoneNumberChangeFM = {
    PhoneCode: int
    PhoneNumber: string
}


[<CLIMutable>]
type RequestEmailChangeFM = {
    Email: string
}


[<CLIMutable>]
type VerifyCodeFM = {
    CodeId: string
    Code: string
}


[<CLIMutable>]
type ChangePasswordFM = {
    CurrentPassword: string
    Password: string
}