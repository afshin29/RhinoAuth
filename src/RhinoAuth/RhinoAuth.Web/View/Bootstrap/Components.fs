module View.Bootstrap.Components

open Giraffe.ViewEngine


// These are our UI contract
// These can be implemented with any CSS library


let _onchange = attr "onchange"

let _role = attr "role"




let ui_class_displayNone = "d-none"

let ui_class_displayInline = "d-inline"

let ui_class_displayInlineBlock = "d-inline-block"

let ui_class_displayBlock = "d-block"


let ui_class_positionStatic = "position-static"

let ui_class_positionRelative = "position-relative"

let ui_class_positionFixed = "position-fixed"

let ui_class_positionSticky = "position-sticky"


let ui_class_textSecondary = "text-secondary"

let ui_class_textDanger = "text-danger"




let ui_section = div [ _class "row" ]


let ui_row = div [ _class "col" ]

let ui_rowCentered = div [ _class "col text-center" ]

let ui_rowEnd = div [ _class "col text-end" ]


let ui_rowHalf = div [ _class "col col-md-6" ]

let ui_rowHalfCentered = div [ _class "col col-md-6 text-center" ]

let ui_rowHalfEnd = div [ _class "col col-md-6 text-end" ]




let private ui_colStackWithGap gap = div [ _class $"d-flex flex-column justify-content-center gap-{gap}" ]

let ui_colStack = ui_colStackWithGap 0

let ui_colStack1 = ui_colStackWithGap 1

let ui_colStack2 = ui_colStackWithGap 2

let ui_colStack3 = ui_colStackWithGap 3



let private ui_rowStackWithGap justify (gap: int) = div [ _class $"d-flex flex-row align-items-center justify-content-{justify} gap-{gap}" ]

let private ui_rowStackStartWithGap = ui_rowStackWithGap "start"

let private ui_rowStackEndWithGap = ui_rowStackWithGap "end"

let private ui_rowStackCenterWithGap = ui_rowStackWithGap "center"

let private ui_rowStackBetweenWithGap = ui_rowStackWithGap "between"

let private ui_rowStackAroundWithGap = ui_rowStackWithGap "around"

let private ui_rowStackEvenlyWithGap = ui_rowStackWithGap "evenly"


let ui_rowStackStart = ui_rowStackStartWithGap 0

let ui_rowStackStart1 = ui_rowStackStartWithGap 1

let ui_rowStackStart2 = ui_rowStackStartWithGap 2

let ui_rowStackStart3 = ui_rowStackStartWithGap 3


let ui_rowStackEnd = ui_rowStackEndWithGap 0

let ui_rowStackEnd1 = ui_rowStackEndWithGap 1

let ui_rowStackEnd2 = ui_rowStackEndWithGap 2

let ui_rowStackEnd3 = ui_rowStackEndWithGap 3


let ui_rowStackCenter = ui_rowStackCenterWithGap 0
                                    
let ui_rowStackCenter1 = ui_rowStackCenterWithGap 1
                                    
let ui_rowStackCenter2 = ui_rowStackCenterWithGap 2
                                    
let ui_rowStackCenter3 = ui_rowStackCenterWithGap 3


let ui_rowStackBetween = ui_rowStackBetweenWithGap 0
                              
let ui_rowStackBetween1 = ui_rowStackBetweenWithGap 1

let ui_rowStackBetween2 = ui_rowStackBetweenWithGap 2

let ui_rowStackBetween3 = ui_rowStackBetweenWithGap 3


let ui_rowStackAround =  ui_rowStackAroundWithGap 0

let ui_rowStackAround1 = ui_rowStackAroundWithGap 1

let ui_rowStackAround2 = ui_rowStackAroundWithGap 2

let ui_rowStackAround3 = ui_rowStackAroundWithGap 3


let ui_rowStackEvenly = ui_rowStackEvenlyWithGap 0

let ui_rowStackEvenly1 = ui_rowStackEvenlyWithGap 1

let ui_rowStackEvenly2 = ui_rowStackEvenlyWithGap 2

let ui_rowStackEvenly3 = ui_rowStackEvenlyWithGap 3



let ui_hr = hr [ _class "my-0" ]

let ui_hr1 = hr [ _class "my-1" ]

let ui_hr2 = hr [ _class "my-2" ]

let ui_hr3 = hr [ _class "my-3" ]

let ui_hrTop1 = hr [ _class "mb-0 mt-1" ]

let ui_hrTop2 = hr [ _class "mb-0 mt-2" ]

let ui_hrTop3 = hr [ _class "mb-0 mt-3" ]

let ui_hrBottom1 = hr [ _class "mt-0 mb-1" ]

let ui_hrBottom2 = hr [ _class "mt-0 mb-2" ]

let ui_hrBottom3 = hr [ _class "mt-0 mb-3" ]

let ui_vr = div [ _class "vr" ] []



let private ui_btn kind size text = button [ _class $"btn btn-{kind}{size}" ] [ str text ]

let ui_button = ui_btn "primary" ""

let ui_buttonSecondary = ui_btn "secondary" ""

let ui_buttonDanger = ui_btn "danger" ""


let ui_buttonSm = ui_btn "primary" " btn-sm"

let ui_buttonSecondarySm = ui_btn "secondary" " btn-sm"

let ui_buttonDangerSm = ui_btn "danger" " btn-sm"


let ui_buttonLg = ui_btn "primary" " btn-lg"

let ui_buttonSecondaryLg = ui_btn "secondary" " btn-lg"

let ui_buttonDangerLg = ui_btn "danger" " btn-lg"


let ui_buttonBlock = ui_btn "primary" " w-100"

let ui_buttonSecondaryBlock = ui_btn "secondary" " w-100"

let ui_buttonDangerBlock = ui_btn "danger" " w-100"



let private ui_linkBtn kind size href text = a [ _class $"btn btn-{kind}{size}"; _href href ] [ str text ]

let ui_linkButton = ui_linkBtn "primary" ""

let ui_linkButtonSecondary = ui_linkBtn "secondary" ""

let ui_linkButtonDanger = ui_linkBtn "danger" ""


let ui_linkButtonSm = ui_linkBtn "primary" " btn-sm"

let ui_linkButtonSecondarySm = ui_linkBtn "secondary" " btn-sm"

let ui_linkButtonDangerSm = ui_linkBtn "danger" " btn-sm"


let ui_linkButtonLg = ui_linkBtn "primary" " btn-lg"

let ui_linkButtonSecondaryLg = ui_linkBtn "secondary" " btn-lg"

let ui_linkButtonDangerLg = ui_linkBtn "danger" " btn-lg"


let ui_linkButtonBlock = ui_linkBtn "primary" " w-100"

let ui_linkButtonSecondaryBlock = ui_linkBtn "secondary" " w-100"

let ui_linkButtonDangerBlock = ui_linkBtn "danger" " w-100"



let ui_icon icon = i [ _class icon ] []

let private ui_iconButtonWithTooltip tooltipPos tooltipText icon =
    button [
        _class "btn btn-icon"
        _data "bs-toggle" "tooltip"
        _data "bs-placement" tooltipPos
        _data "bs-title" tooltipText
    ] [
        ui_icon icon 
    ]

let private ui_iconLinkWithTooltip tooltipPos href tooltipText icon =
    a [
        _href href
        _class "btn btn-icon"
        _data "bs-toggle" "tooltip"
        _data "bs-placement" tooltipPos
        _data "bs-title" tooltipText
    ] [
        ui_icon icon
    ]


let ui_iconButton icon = button [ _class "btn btn-icon" ] [ ui_icon icon ]

let ui_iconLink href icon = a [ _href href; _class "btn btn-icon" ] [ ui_icon icon ]


let ui_iconButtonBottom = ui_iconButtonWithTooltip "bottom"

let ui_iconButtonTop = ui_iconButtonWithTooltip "top"

let ui_iconButtonLeft = ui_iconButtonWithTooltip "left"

let ui_iconButtonRight = ui_iconButtonWithTooltip "right"


let ui_iconLinkBottom = ui_iconLinkWithTooltip "bottom"

let ui_iconLinkTop = ui_iconLinkWithTooltip "top"

let ui_iconLinkLeft = ui_iconLinkWithTooltip "left"
           
let ui_iconLinkRight = ui_iconLinkWithTooltip "right"



let ui_cardWithHeader header body =
    div [
        _class "card"
    ] [
        div [ _class "card-header" ] header

        div [ _class "card-body" ] body 
    ]

let ui_card body =
    div [
        _class "card"
    ] [
        div [ _class "card-body" ] body 
    ]



let private ui_alert kind text = div [ _class $"alert alert-{kind}"; _role "alert" ] [ str text ]

let ui_alertSuccess = ui_alert "success"

let ui_alertDanger = ui_alert "danger"

let ui_alertWarning = ui_alert "warning"



let ui_modal id title body =
    div [
        _class "modal fade"
        _id id
        _tabindex "-1"
    ] [
        div [
            _class "modal-dialog"
        ] [
            div [
                _class "modal-content"
            ] [
                div [
                    _class "modal-header"
                ] [
                    h5 [ _class "modal-title" ] [ str title ]

                    button [
                        _type "button"
                        _class "btn-close"
                        _data "bs-dismiss" "modal"
                    ] []
                ]

                div [ _class "modal-body" ] body
            ]
        ]
    ]


let ui_modalOpener id text =
    button [
        _type "button"
        _class "btn btn-primary"
        _data "bs-toggle" "modal"
        _data "bs-target" $"#{id}"
    ] [
        str text
    ]


let ui_modalOpenerIcon id icon =
    button [
        _type "button"
        _class "btn btn-icon"
        _data "bs-toggle" "modal"
        _data "bs-target" $"#{id}"
    ] [
        ui_icon icon
    ]


let private ui_modalOpenerIconWithTooltip tooltipPos (id: string) tooltipText icon =
    button [
        _type "button"
        _class "btn btn-icon"
        _data "bs-toggle" "modal"
        _data "bs-target" $"#{id}"
    ] [
        i [
            _class icon
            _data "bs-toggle" "tooltip"
            _data "bs-placement" tooltipPos
            _data "bs-title" tooltipText
        ] []
    ]


let ui_modalOpenerIconBottom = ui_modalOpenerIconWithTooltip "bottom"

let ui_modalOpenerIconTop = ui_modalOpenerIconWithTooltip "top"

let ui_modalOpenerIconLeft = ui_modalOpenerIconWithTooltip "left"

let ui_modalOpenerIconRight = ui_modalOpenerIconWithTooltip "right"



let ui_form_label text = label [ _class "form-label" ] [ str text ]

let ui_form_hint (text: string) = div [ _class "form-text" ] [ str $"• {text}" ]


let ui_form_inputHidden value name =
    input [
        _type "hidden"
        _name name
        _value value
    ]

let ui_form_antiForgery token = View.ViewConstants.AntiforgeryInputName |> ui_form_inputHidden token


let private ui_form_input kind attributes value name =
    input ([
        _class "form-control"
        _type kind
        _name name

        if Option.isSome value then
            _value value.Value
    ] @ attributes)

let private ui_form_inputText = ui_form_input "text"

let private ui_form_inputEmail = ui_form_input "email"

let private ui_form_inputPassword = ui_form_input "password"

let private ui_form_inputPhone = ui_form_input "tel"


let private ui_form_checkbox text (isSet: bool) name =
    div [
        _class "form-check"
    ] [
        label [
            _class "form-check-label"
        ] [
            name |> ui_form_inputHidden (isSet.ToString())

            input [
                _class "form-check-input"
                _type "checkbox"
                _onclick "this.previousElementSibling.value = this.checked"

                if isSet then
                    _checked
            ]

            str text
        ]
    ]


let private ui_form_select (options: seq<string * string * bool * bool>) text name =
    [
        ui_form_label text

        select [
            _class "form-select"
            _name name
        ] (
            options
            |> Seq.map (fun (label, value, selected, disabled) ->
                option [
                    _value value
        
                    if selected then _selected
                
                    if disabled then _disabled
                ] [
                    str label
                ]
            )
            |> Seq.toList
        )
    ]



let private ui_form_inputRowWithHint row inputElm uiHints attributes text value name =
    row ([
        ui_form_label text

        inputElm attributes value name
    ] @ (uiHints |> List.map ui_form_hint))

let private ui_form_inputRowFullWithHint = ui_form_inputRowWithHint ui_row

let private ui_form_inputRowHalfWithHint = ui_form_inputRowWithHint ui_rowHalf


let ui_form_inputRowTextWithHint = ui_form_inputRowFullWithHint ui_form_inputText

let ui_form_inputRowHalfTextWithHint = ui_form_inputRowHalfWithHint ui_form_inputText

let ui_form_inputRowEmailWithHint = ui_form_inputRowFullWithHint ui_form_inputEmail

let ui_form_inputRowHalfEmailWithHint = ui_form_inputRowHalfWithHint ui_form_inputEmail

let ui_form_inputRowPasswordWithHint = ui_form_inputRowFullWithHint ui_form_inputPassword

let ui_form_inputRowHalfPasswordWithHint = ui_form_inputRowHalfWithHint ui_form_inputPassword

let ui_form_inputRowPhoneWithHint = ui_form_inputRowFullWithHint ui_form_inputPhone

let ui_form_inputRowHalfPhoneWithHint = ui_form_inputRowHalfWithHint ui_form_inputPhone


let ui_form_inputRowText = ui_form_inputRowTextWithHint []

let ui_form_inputRowHalfText = ui_form_inputRowHalfTextWithHint []


let ui_form_inputRowEmail = ui_form_inputRowEmailWithHint []

let ui_form_inputRowHalfEmail = ui_form_inputRowHalfEmailWithHint []


let ui_form_inputRowPassword = ui_form_inputRowPasswordWithHint []

let ui_form_inputRowHalfPassword = ui_form_inputRowHalfPasswordWithHint []


let ui_form_inputRowPhone = ui_form_inputRowPhoneWithHint []

let ui_form_inputRowHalfPhone = ui_form_inputRowHalfPhoneWithHint []


let ui_form_checkboxRow text isSet name = [ ui_form_checkbox text isSet name ] |> ui_row

let ui_form_checkboxRowHalf text isSet name = [ ui_form_checkbox text isSet name ] |> ui_rowHalf


let ui_form_selectRow options text name = ui_row <| ui_form_select options text name

let ui_form_selectRowHalf options text name = ui_rowHalf <| ui_form_select options text name



let private ui_form method action = form [ _action action; _method method ]

let ui_formGet = ui_form "get"

let ui_formPost = ui_form "post"


let ui_formPostTemplate action afToken errorMsg body =
    ui_formPost action [
        if Option.isSome errorMsg then
            ui_alertDanger errorMsg.Value

        ui_form_antiForgery afToken

        div [ _class "row row-cols-1 g-3" ] body
    ]

let ui_formPostSubmitTemplate action text afToken errorMsg body =
    ui_formPostTemplate action afToken errorMsg (body @ [ ui_row [ ui_buttonBlock text ] ])




let ui_profilePic avatar size =
    img [
        _class "rounded-circle border-1 mx-3"
        _width size
        _height size
    
        let path =
            avatar
            |> Option.map (fun fileName -> $"/img/profile-pics/{fileName}.jpg")
            |> Option.defaultValue "/img/profile-pic.jpg"
        
        _src path
    ]


let ui_countryFlag code =
    img [
        _class "rounded mx-2"
        _width "20"
        _height "20"
        _src $"/img/flags/1x1/{code}.svg"
    ]