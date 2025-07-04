module View.Pages.Login

open Giraffe.ViewEngine
open View

open View.UiHelper
open View.InputAttributes
open View.Bootstrap.Components
open View.Bootstrap.Layouts



let func (formModel: FormModels.LoginFM option) errorMsg viewData =

    let afToken = viewData.AntiforgeryTokenProvider()
    let returnUrl = viewData.QueryParameterProvider "returnUrl"

    let login = Unchecked.defaultof<FormModels.LoginFM>

    ui_formPostTemplate (getPathWithReturnUrl "/login" returnUrl) afToken errorMsg [
        
        nameof login.Username |> ui_form_inputRowText usernameAttributes "Username" (formModel |> Option.map _.Username)

        nameof login.Password |> ui_form_inputRowPassword passwordAttributes "Password" None

        nameof login.RemeberMe |> ui_form_checkboxRowHalf "Remember Me" (formModel |> Option.map _.RemeberMe.IsSome |> Option.defaultValue false)

        ui_rowHalfEnd [
            a [
                _href "/forgot-password"
            ] [
                str "forgot password"
            ]
        ]

        ui_row [
            ui_buttonBlock "Login"
        ]
            
        ui_rowCentered [
            str "New user? "
            a [
                _href <| getPathWithReturnUrl "/signup" returnUrl
            ] [
                str "Signup"
            ]
        ]
    ]
    |> cardLayout viewData "Login"