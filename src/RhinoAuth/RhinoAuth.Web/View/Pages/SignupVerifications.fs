module View.Pages.SignupVerifications

open Giraffe.ViewEngine
open View

open View.UiHelper
open View.InputAttributes
open View.Bootstrap.Components
open View.Bootstrap.Layouts



let private signupVerificationForm action title afToken signupId returnUrl errorMsg =
    
    let model = Unchecked.defaultof<FormModels.SignupVerificationFM>

    ui_formPostSubmitTemplate (getPathWithReturnUrl $"{action}?signupId={signupId}" returnUrl) "Submit" afToken errorMsg [
        h3 [] [ str title ]

        nameof model.SignupId |> ui_form_inputHidden signupId

        nameof model.Code |> ui_form_inputRowText [ _type "text"; _required ] "Code" None
    ]


let verifyEmail _ errorMsg viewData =

    let afToken = viewData.AntiforgeryTokenProvider()
    let signupId = viewData.QueryParameterProvider "signupId" |> Option.defaultValue ""
    let returnUrl = viewData.QueryParameterProvider "returnUrl"
    
    signupVerificationForm "/signup/verify-email" "Email Verification" afToken signupId returnUrl errorMsg
    |> cardLayout viewData "Verify Email"


let verifyPhone _ errorMsg viewData =

    let afToken = viewData.AntiforgeryTokenProvider()
    let signupId = viewData.QueryParameterProvider "signupId" |> Option.defaultValue ""
    let returnUrl = viewData.QueryParameterProvider "returnUrl"

    signupVerificationForm "/signup/verify-phone" "Phone Number Verification" afToken signupId returnUrl errorMsg
    |> cardLayout viewData "Verify Phone Number"