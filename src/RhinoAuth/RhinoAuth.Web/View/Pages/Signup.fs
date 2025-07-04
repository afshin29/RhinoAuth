module View.Pages.Signup

open Giraffe.ViewEngine
open View
open RhinoAuth.Database
open System.Collections.Generic

open View.UiHelper
open View.InputAttributes
open View.Bootstrap.Components
open View.Bootstrap.Layouts

let func (countryList: IReadOnlyDictionary<string, Country>) ipCountryCode (formModel: FormModels.SignupFM option) errorMsg viewData =

    let afToken = viewData.AntiforgeryTokenProvider()
    let returnUrl = viewData.QueryParameterProvider "returnUrl"

    let signup = Unchecked.defaultof<FormModels.SignupFM>

    let selectedCode = formModel |> Option.map _.PhoneCode.ToString()

    let countryOptions =
        countryList
        |> Seq.sortBy _.Value.Name
        |> Seq.map (fun (c) ->
            let label = $"{c.Value.Name} (+{c.Value.PhoneCode})"
            
            let value =c.Value.PhoneCode.ToString()
            
            let selected =
                match (selectedCode, ipCountryCode) with
                | (Some value, _) when c.Value.PhoneCode.ToString() = value -> true
                | (None, value) when c.Value.Code = value -> true
                | _ -> false
            
            let disabled = not c.Value.AllowPhoneNumberResgistration

            (label, value, selected, disabled)
        )

    ui_formPostTemplate "/signup" afToken errorMsg [

        nameof signup.FirstName |> ui_form_inputRowHalfText nameAttributes "First Name (English)" (formModel |> Option.map _.FirstName)

        nameof signup.LastName |> ui_form_inputRowHalfText nameAttributes "Last Name (English)" (formModel |> Option.map _.LastName)

        nameof signup.PhoneCode |> ui_form_selectRowHalf countryOptions "Phone Code"

        nameof signup.PhoneNumber |> ui_form_inputRowHalfPhone phoneNumberAttributes "Phone Number" (formModel |> Option.map _.PhoneNumber)

        nameof signup.Email |> ui_form_inputRowEmail emailAttributes "Email" (formModel |> Option.map _.Email)
        
        nameof signup.Username |> ui_form_inputRowTextWithHint usernameUiHints usernameAttributes "Username" (formModel |> Option.map _.Username)

        nameof signup.Password |> ui_form_inputRowPasswordWithHint passwordUiHints passwordAttributes "Password" None

        if viewData.IsDevEnv then
            input [
                _type "hidden"
                _name "Captcha"
                _value "123"
            ]
        else
            div [] [ str "Captcha goes here" ]
            
        ui_row [ ui_buttonBlock "Signup" ]

        ui_rowCentered [
            str "Already signed up? "
            a [
                _href <| getPathWithReturnUrl "/login" returnUrl
            ] [
                str "Login"
            ]
        ]
    ]
    |> cardLayoutWithAssets [ (* include captcha script here *) ] viewData "Signup"