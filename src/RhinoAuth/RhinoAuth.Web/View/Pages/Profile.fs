module View.Pages.Profile

open Giraffe.ViewEngine
open View

open View.UiHelper
open View.InputAttributes
open View.Bootstrap.Components
open View.Bootstrap.Layouts



let func (profileVM: ViewModels.ProfileVM) _ errorMsg viewData =

    let afToken = viewData.AntiforgeryTokenProvider()

    ui_section [
        ui_rowHalf [
            if Option.isSome errorMsg then
                ui_alertDanger errorMsg.Value

            ui_cardWithHeader [
                ui_rowStackStart3 [
                    ui_colStack2 [
                        ui_profilePic profileVM.Avatar "65"

                        ui_rowStackAround [
                            form [
                                _action "/change-avatar"
                                _method "post"
                                _enctype "multipart/form-data"
                            ] [
                                ui_form_antiForgery afToken

                                input [
                                    _class ui_class_displayNone
                                    _id "avatar-input"
                                    _type "file"
                                    _name "Avatar"
                                    _accept "image/jpeg"
                                    _onchange "this.form.submit()"
                                ]

                                label [
                                    _for "avatar-input"
                                ] [
                                    ui_iconLinkLeft "" "Change" "iconoir-edit-pencil"
                                    |> UiHelper.removeAttribute "href"
                                ]
                            ]
                            
                            ui_formPost "/remove-avatar" [
                                ui_form_antiForgery afToken

                                ui_iconButtonRight "Remove" "iconoir-trash"
                                |> UiHelper.addClass ui_class_textDanger
                                |> UiHelper.disableIf profileVM.Avatar.IsNone
                            ]
                        ]
                    ]

                    div [ _class "vr" ] []

                    div [] [
                        h4 [] [
                            str $"{profileVM.FirstName} {profileVM.LastName}"

                            ui_iconLinkRight "/profile/change-name" "Edit" "iconoir-edit"
                        ]

                        h5 [
                            _class ui_class_textSecondary
                        ] [
                            str $"@{profileVM.Username}"
                        ]
                    ]
                ]
                |> UiHelper.addClass "py-3"
            ] [
                ui_colStack2 [
                    div [] [
                        strong [] [ str "Phone Number: " ]
                        span [] [
                            ui_countryFlag profileVM.CountryCode
                    
                            str $"+{profileVM.CountryPhoneCode} {profileVM.PhoneNumber}"
                        ]
                        ui_iconLinkRight "/profile/change-phone" "Edit" "iconoir-edit"
                    ]
                    
                    div [] [
                        strong [] [ str "Email: " ]
                        str profileVM.Email
                        ui_iconLinkRight "/profile/change-email" "Edit" "iconoir-edit"
                    ]
                ]

                ui_linkButtonSm "/profile/change-password" "Change Password"
                |> UiHelper.addClass "mt-2"

                ui_hr3

                ui_linkButtonDangerSm "/profile/delete-account" "Delete Account"
            ]
        ]
    ]
    |> mainLayout viewData "Profile"