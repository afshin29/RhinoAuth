module View.Pages.PasswordRecovery

open Giraffe.ViewEngine
open View

open View.UiHelper
open View.InputAttributes
open View.Bootstrap.Components
open View.Bootstrap.Layouts



let forgotPassword (formModel: FormModels.ForgotPasswordFM option) errorMsg viewData =

    let afToken = viewData.AntiforgeryTokenProvider()
    let returnUrl = viewData.QueryParameterProvider "returnUrl"

    let fpModel = Unchecked.defaultof<FormModels.ForgotPasswordFM>
    
    ui_formPostSubmitTemplate (getPathWithReturnUrl "/forgot-password" returnUrl) "Send Code" afToken errorMsg [
        h3 [] [ str "Forgot Password" ]

        nameof fpModel.Username |> ui_form_inputRowText usernameAttributes "Username" (formModel |> Option.map _.Username)

        if viewData.IsDevEnv then
            input [
                _type "hidden"
                _name "Captcha"
                _value "123"
            ]
        else
            div [] [ str "Captcha goes here" ]
    ]
    |> cardLayoutWithAssets [ (* include captcha script here *) ] viewData "Forgot Password"


let resetPassword _ errorMsg viewData =

    let afToken = viewData.AntiforgeryTokenProvider()
    let returnUrl = viewData.QueryParameterProvider "returnUrl"
    let codeId = viewData.QueryParameterProvider "codeId" |> Option.defaultValue ""

    let fpModel = Unchecked.defaultof<FormModels.ResetPasswordFM>
    
    ui_formPostSubmitTemplate(getPathWithReturnUrl $"/reset-password?codeId={codeId}" returnUrl) afToken "Submit" errorMsg [
        h3 [] [ str "Reset Password" ]

        div [] [ str "Check your email and enter the code and your new password." ]

        nameof fpModel.CodeId |> ui_form_inputHidden codeId

        nameof fpModel.Code |> ui_form_inputRowText [ _type "text"; _required ] "Code" None

        nameof fpModel.Password |> ui_form_inputRowPassword passwordAttributes "New Password" None
    ]
    |> cardLayout viewData "Reset Password"