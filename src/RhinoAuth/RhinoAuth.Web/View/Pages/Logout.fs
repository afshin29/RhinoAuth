module View.Pages.Logout

open Giraffe.ViewEngine
open View

open View.Bootstrap.Components
open View.Bootstrap.Layouts


let func viewData =

    let afToken = viewData.AntiforgeryTokenProvider()

    let returnUrl = viewData.QueryParameterProvider "post_logout_redirect_uri" |> Option.defaultValue ""
    let idTokenHint = viewData.QueryParameterProvider "id_token_hint" |> Option.defaultValue ""
    let clientId = viewData.QueryParameterProvider "client_id" |> Option.defaultValue ""
    let state = viewData.QueryParameterProvider "state" |> Option.defaultValue ""

    ui_row [
        h3 [] [ str "Logout" ]

        p [] [ str "Logout from here and all external logins on this browser." ]

        ui_formPost "/oidc-logout" [

            ui_form_antiForgery afToken

            ui_form_inputHidden returnUrl "post_logout_redirect_uri"
            ui_form_inputHidden idTokenHint "id_token_hint"
            ui_form_inputHidden clientId "client_id"
            ui_form_inputHidden state "state"

            ui_buttonDanger "Logout"
        ]
    ]
    |> mainLayout viewData "Home Page"