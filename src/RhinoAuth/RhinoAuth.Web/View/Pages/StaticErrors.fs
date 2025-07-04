module View.Pages.StaticErrors

open Giraffe.ViewEngine

open View.Bootstrap.Components
open View.Bootstrap.Layouts


let notFound viewData =
    ui_rowCentered [
        h1 [
            _class "pt-5"
        ] [
            str "404"
        ]

        h3 [
            _class "py-5"
        ] [
            str "The page you are looking for does not exist."
        ]

        ui_hr
    ]
    |> mainLayout viewData "Home Page"


let internalServerError viewData =
    ui_rowCentered [
        h1 [
            _class "pt-5"
        ] [
            str "500"
        ]

        h3 [
            _class "py-5"
        ] [
            str "Unfortunately the request ran into an error."
        ]

        ui_hr
    ]
    |> mainLayout viewData "Home Page"


let serviceUnavailable viewData =
    ui_rowCentered [
        h1 [
            _class "pt-5"
        ] [
            str "503"
        ]

        h3 [
            _class "py-5"
        ] [
            str "Unfortunately service is currently unavailable."
        ]

        ui_hr
    ]
    |> mainLayout viewData "Home Page"