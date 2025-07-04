module View.Pages.Home

open Giraffe.ViewEngine

open View.Bootstrap.Components
open View.Bootstrap.Layouts


// Ideally I wanted to have a "Pages" module across multiple files
// and name each function after its page name, like "Pages.home"
// but unfortunatelly F# does not allow partial modules
// so I just name these default functions "func"

let func viewData =
    ui_rowCentered [
        h1 [
            _class "py-5"
        ] [
            str "Welcome to RhinoAuth!"
        ]

        ui_hr
    ]
    |> mainLayout viewData "Home Page"