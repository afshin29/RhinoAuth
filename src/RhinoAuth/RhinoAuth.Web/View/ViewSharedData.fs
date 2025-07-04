namespace View

open Giraffe.ViewEngine
open System.Security.Claims



type ViewData = {
    CurrentPath: string
    User: ClaimsPrincipal
    IsDarkMode: bool
    Language: string
    IsRTL: bool
    AntiforgeryTokenProvider: (unit -> string)
    QueryParameterProvider: (string -> string option)
    IsDevEnv: bool
}



module ViewConstants =
    
    [<Literal>]
    let AntiforgeryInputName = "_af_token"



module InputAttributes =

    let nameAttributes = [
        _type "text"
        _required
        _minlength (FormValidations.nameMinLength.ToString())
        _maxlength (FormValidations.nameMaxLength.ToString())
    ]
    
    let emailAttributes = [
        _type "email"
        _required
    ]
    
    let usernameUiHints = [
        $"{FormValidations.usernameMinLength} to {FormValidations.usernameMaxLength} characters"
        "Start with a letter, use letters, numbers and underscore"
    ]
    
    let usernameAttributes = [
        _type "text"
        _required
        _minlength (FormValidations.usernameMinLength.ToString())
        _maxlength (FormValidations.usernameMaxLength.ToString())
        _pattern FormValidations.usernameRegex
    ]
    
    let passwordUiHints = [ $"{FormValidations.passwordMinLength} to {FormValidations.passwordMaxLength} charachters" ]
    
    let passwordAttributes = [
        _type "password"
        _required
        _minlength (FormValidations.passwordMinLength.ToString())
        _maxlength (FormValidations.passwordMaxLength.ToString())
    ]
    
    let phoneNumberAttributes = [
        _type "tel"
        _required
        _maxlength (FormValidations.phoneNumberMaxLength.ToString())
        _pattern FormValidations.phoneNumberRegex
        _placeholder FormValidations.phoneNumberPlaceholder
    ]



module UiHelper =

    let getPathWithReturnUrl (path: string) (returnUrl: string option) =
        match returnUrl with
        | Some url when path.Contains("?") -> $"{path}&returnUrl={url}"
        | Some url -> $"{path}?returnUrl={url}"
        | _ -> path

    
    let private addAttrElm key value ((name, attr): XmlElement) =

        let mapper = function
            | KeyValue (k, v) when k = key -> KeyValue(key, $"{v} {value}")
            | other -> other

        XmlElement(name, attr |> Array.map mapper)
    
    let private addAttrNode key value = function
        | ParentNode (elm, list) -> ParentNode(elm |> addAttrElm key value, list)
        | VoidElement elm -> VoidElement(elm |> addAttrElm key value)
        | other -> other
    
    let addClass value = addAttrNode "class" value

    let addStyle value = addAttrNode "style" value


    let private removeAttrElm key ((name, attr): XmlElement) =

        let filter = function
            | KeyValue (k, v) when k = key -> false
            | _ -> true

        XmlElement(name, attr |> Array.filter filter)
    
    let removeAttribute key = function
        | ParentNode (elm, list) -> ParentNode(elm |> removeAttrElm key, list)
        | VoidElement elm -> VoidElement(elm |> removeAttrElm key)
        | other -> other

    
    let private disableElmIf value ((name, attr): XmlElement) =
        let newAttr = if value then attr |> Array.append [| _disabled |] else attr
        XmlElement(name, newAttr)

    let disableIf value = function
        | ParentNode (elm, list) -> ParentNode(elm |> disableElmIf value, list)
        | VoidElement elm -> VoidElement(elm |> disableElmIf value)
        | other -> other