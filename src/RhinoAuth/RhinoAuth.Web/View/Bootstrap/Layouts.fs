module View.Bootstrap.Layouts

open Giraffe.ViewEngine
open View

open View.Bootstrap.Components


let emptyLayout pageAssets viewData pageTitle pageBody =
    html [
        _lang viewData.Language

        let theme = if viewData.IsDarkMode then "dark" else "light"
        _data "bs-theme" theme
    ] [
        head [] ([
            meta [ _charset "utf8" ]
            meta [ _name "viewport"; _content "width=device-width, initial-scale=1.0" ]

            ``base`` [ _href "/" ]

            link [
                _href "https://cdn.jsdelivr.net/npm/bootstrap@5.3.5/dist/css/bootstrap.min.css";
                _rel "stylesheet";
                _integrity "sha384-SgOJa3DmI69IUzQ2PVdRZhwQ+dy64/BUtbMJw1MZ8t5HZApcHrRKUc4W0kG879m7";
                _crossorigin "anonymous"
            ]

            link [ _rel "stylesheet"; _href "https://cdn.jsdelivr.net/npm/bootstrap-icons@1.11.3/font/bootstrap-icons.min.css" ]
            link [ _rel "stylesheet"; _href "css/iconoir.css" ]
            link [ _rel "stylesheet"; _href "css/app.css" ]
            
            link [ _rel "preconnect"; _href "https://fonts.googleapis.com" ]
            link [ _rel "preconnect"; _href "https://fonts.gstatic.com"; _crossorigin "" ]
            link [ _href "https://fonts.googleapis.com/css2?family=Jost:ital,wght@0,100..900;1,100..900&display=swap"; _rel "stylesheet" ]
        
            title [] [ str (pageTitle + " | RhinoAuth") ]
        ] @ pageAssets)

        body [] [
            pageBody

            script [
                _src "https://cdn.jsdelivr.net/npm/bootstrap@5.3.5/dist/js/bootstrap.bundle.min.js";
                _integrity "sha384-k6d4wzSIapyDyv1kpU366/PK5hCdSbCRGRCMv+eplOQJWyd1fbcAu9OCUj5zNLiq";
                _crossorigin "anonymous"
            ] []

            script [ _src "js/app.js"] []
        ]
    ]



let mainHeader viewData hideLogin =
    header [
        _class "pt-2 mb-3"
    ] [
        ui_rowStackBetween [
            a [
                _href "/"
                _class "text-decoration-none"
                _style "color: inherit"
            ] [
                ui_rowStackBetween2 [
                    img [
                        _src "img/rhino_logo2.png"
                        _style "max-height: 50px"
                        _title "logo"
                    ]

                    h3 [
                        _class "m-0"
                    ] [ 
                        str "RhinoAuth"
                    ]
                ]
            ]

            ui_rowStackEvenly2 [
                ui_formPost "/switch-theme" [
                    ui_form_antiForgery <| viewData.AntiforgeryTokenProvider()

                    ui_form_inputHidden viewData.CurrentPath "current_path"

                    let themeIcon = if viewData.IsDarkMode then "iconoir-sun-light" else "iconoir-half-moon"
                    ui_iconButtonBottom "Toggle Theme" themeIcon
                ]
                
                if viewData.User.IsLoggedIn() then
                    ui_iconLinkBottom "/profile" "Profile" "iconoir-profile-circle"
                    
                    ui_iconLinkBottom "/oidc-logout" "Logout" "iconoir-log-out"

                elif not hideLogin then
                    ui_iconLinkBottom "/login" "Login" "iconoir-log-in"
            ]
        ]

        ui_hrTop2
    ]



let mainFooter =
    footer [
        _class "pb-2 mt-3"
    ] [
        ui_hrBottom2

        ui_rowStackBetween [
            span [] [ str $"© {System.DateTime.Now.Year.ToString()}" ]

            div [] [
                a [
                    _href "https://getbootstrap.com"
                    _target "_blank"
                ] [
                    str "Bootstrap"
                ]

                span [ _class "px-2" ] [ str "•" ]
                
                a [
                    _href "https://iconoir.com"
                    _target "_blank"
                ] [
                    str "Iconoir"
                ]

                span [ _class "px-2" ] [ str "•" ]

                a [
                    _href "https://github.com/afshin29/RhinoAuth"
                    _target "_blank"
                ] [
                    str "Github"
                ]
            ]
        ]
    ]



let mainLayoutWithAssets pageAssets viewData pageTitle pageBody =
    div [
        _class "container-fluid min-vh-100 d-flex flex-column justify-content-between"
        _style "max-width: 960px"
    ] [
        div [] [
            mainHeader viewData false

            main [] [ pageBody ]
        ]

        mainFooter
    ]
    |> emptyLayout pageAssets viewData pageTitle



let mainLayout = mainLayoutWithAssets []



let cardLayoutWithAssets pageAssets viewData pageTitle pageBody =
    div [
        _class "container min-vh-100 d-flex flex-column justify-content-center align-items-center"
    ] [
        div [
            _class "card shadow px-3 pb-3"
            _style "max-width: 560px"
        ] [
            mainHeader viewData true

            main [] [ pageBody ]
        ]
    ]
    |> emptyLayout pageAssets viewData pageTitle


let cardLayout = cardLayoutWithAssets []