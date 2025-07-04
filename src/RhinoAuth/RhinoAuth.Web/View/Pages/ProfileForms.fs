module View.Pages.ProfileForms

open Giraffe.ViewEngine
open RhinoAuth.Database
open System.Collections.Generic
open View

open View.UiHelper
open View.InputAttributes
open View.Bootstrap.Components
open View.Bootstrap.Layouts


let changeName (formModel: FormModels.ChangeNameFM option) errorMsg viewData =

    let afToken = viewData.AntiforgeryTokenProvider()

    let model = Unchecked.defaultof<FormModels.ChangeNameFM>

    ui_section [
        ui_rowHalf [
            ui_cardWithHeader [
                ui_rowStackStart2 [
                    ui_iconLink "/profile" "iconoir-arrow-left-circle"
                    h5 [ _class "m-0" ] [ str "Change Profile Name" ]
                ]
            ] [
                ui_formPostSubmitTemplate "/profile/change-name" "Submit" afToken errorMsg [
                
                    nameof model.FirstName |> ui_form_inputRowText nameAttributes "First Name (English)" (formModel |> Option.map _.FirstName)
                
                    nameof model.LastName |> ui_form_inputRowText nameAttributes "Last Name (English)" (formModel |> Option.map _.LastName)
                ]
            ]
        ]
    ]
    |> mainLayout viewData "Change name"


let changePhoneNumber (countryList: IReadOnlyDictionary<string, Country>) (formModel: FormModels.RequestPhoneNumberChangeFM option) errorMsg viewData =

    let afToken = viewData.AntiforgeryTokenProvider()

    let model = Unchecked.defaultof<FormModels.RequestPhoneNumberChangeFM>

    let selectedCode =
        formModel
        |> Option.map _.PhoneCode
        |> Option.defaultValue 0

    let countryOptions =
        countryList
        |> Seq.sortBy _.Value.Name
        |> Seq.map (fun (c) ->
            let label = $"{c.Value.Name} (+{c.Value.PhoneCode})"
            
            let value = c.Value.PhoneCode.ToString()
            
            let selected = c.Value.PhoneCode = selectedCode
            
            let disabled = not c.Value.AllowPhoneNumberResgistration

            (label, value, selected, disabled)
        )

    ui_section [
        ui_rowHalf [
            ui_cardWithHeader [
                ui_rowStackStart2 [
                    ui_iconLink "/profile" "iconoir-arrow-left-circle"
                    h5 [ _class "m-0" ] [ str "Change Phone Number" ]
                ]
            ] [
                ui_formPostSubmitTemplate "/profile/change-phone" "Send Code" afToken errorMsg [
                
                    nameof model.PhoneCode |> ui_form_selectRow countryOptions "Phone Code"
                
                    nameof model.PhoneNumber |> ui_form_inputRowPhone phoneNumberAttributes "Phone Number" (formModel |> Option.map _.PhoneNumber)
                ]
            ]
        ]
    ]
    |> mainLayout viewData "Change phone number"


let changeEmail (formModel: FormModels.RequestEmailChangeFM option) errorMsg viewData =

    let afToken = viewData.AntiforgeryTokenProvider()

    let model = Unchecked.defaultof<FormModels.RequestEmailChangeFM>

    ui_section [
        ui_rowHalf [
            ui_cardWithHeader [
                ui_rowStackStart2 [
                    ui_iconLink "/profile" "iconoir-arrow-left-circle"
                    h5 [ _class "m-0" ] [ str "Change Email" ]
                ]
            ] [
                ui_formPostSubmitTemplate "/profile/change-email" "Send Code" afToken errorMsg [
                
                    nameof model.Email |> ui_form_inputRowEmail emailAttributes "Email" (formModel |> Option.map _.Email)            
                ]
            ]
        ]
    ]
    |> mainLayout viewData "Change email"


let verifyCode actionName (formModel: FormModels.VerifyCodeFM option) errorMsg viewData =

    let afToken = viewData.AntiforgeryTokenProvider()
    let codeId = viewData.QueryParameterProvider "codeId" |> Option.defaultValue ""

    let model = Unchecked.defaultof<FormModels.VerifyCodeFM>

    ui_section [
        ui_rowHalf [
            ui_cardWithHeader [
                h5 [ _class "m-0" ] [ str "Verify Code" ]
            ] [
                ui_formPostSubmitTemplate $"/profile/verify-{actionName}-code?codeId={codeId}" "Submit" afToken errorMsg [
                    div [] [ str $"Check your {actionName} and enter the code." ]

                    nameof model.CodeId |> ui_form_inputHidden codeId

                    nameof model.Code |> ui_form_inputRowText [ _type "text"; _required ] "Code" None
                ]
            ]
        ]
    ]
    |> mainLayout viewData "Verify code"


let changePassword (formModel: FormModels.ChangePasswordFM option) errorMsg viewData =

    let afToken = viewData.AntiforgeryTokenProvider()

    let model = Unchecked.defaultof<FormModels.ChangePasswordFM>

    ui_section [
        ui_rowHalf [
            ui_cardWithHeader [
                ui_rowStackStart2 [
                    ui_iconLink "/profile" "iconoir-arrow-left-circle"
                    h5 [ _class "m-0" ] [ str "Change Password" ]
                ]
            ] [
                ui_formPostSubmitTemplate $"/profile/change-password" "Submit" afToken errorMsg [

                    nameof model.CurrentPassword |> ui_form_inputRowPassword [ _type "password"; _required ] "Current Password" None
                    
                    nameof model.Password |> ui_form_inputRowPasswordWithHint passwordUiHints passwordAttributes "New Password" None
                ]
            ]
        ]
    ]
    |> mainLayout viewData "Change password"