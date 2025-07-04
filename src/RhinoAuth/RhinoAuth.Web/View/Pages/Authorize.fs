module View.Pages.Authorize

open Giraffe.ViewEngine
open View

open View.Bootstrap.Components
open View.Bootstrap.Layouts


let unprocessableRequest viewData =
    ui_row [
        h5 [
            _class "py-3"
        ] [
            str $"[422] Unprocessable Request"
        ]

        div [] [
            str "At the very least, the following parameters are required in order to process the request:"

            ul [] [
                li [] [ str "response_type"]
                li [] [ str "client_id"]
                li [] [ str "code_challenge"]
            ]
        ]
    ]
    |> mainLayout viewData "OAuth Authorization"


let invalidClient viewData =
    ui_row [
        h5 [
            _class "py-3"
        ] [
            str $"[400] Bad Request"
        ]

        div [] [
            str "The provided 'client_id' is invalid."
        ]
    ]
    |> mainLayout viewData "OAuth Authorization"


let consent (consentVM: OAuthModels.ConsentVM) _ errorMsg viewData =

    let afToken = viewData.AntiforgeryTokenProvider()

    ui_section [
        ui_rowHalf [
            ui_cardWithHeader [
                h5 [ _class "m-0" ] [ str "Authorization Consent" ]
            ] [
                if Option.isSome errorMsg then
                    ui_alertDanger errorMsg.Value

                p [] [ str $"Do you want to login on {consentVM.ClientName} with your account?"]

                ui_rowStackStart2 [
                    ui_formPost $"/oauth/authorize/accept?code={consentVM.Code}" [
                    
                        ui_form_antiForgery afToken

                        ui_form_inputHidden consentVM.Code "Code"
                    
                        ui_button "Accept"
                    ]
                    
                    ui_formPost $"/oauth/authorize/reject?code={consentVM.Code}" [
                    
                        ui_form_antiForgery afToken
                        
                        ui_form_inputHidden consentVM.Code "Code"
                    
                        ui_buttonDanger "Reject"
                    ]
                ]
            ]
        ]
    ]
    |> mainLayout viewData "OAuth Authorization"