module Utilities.PhoneNumberValidator

open PhoneNumbers


let isPhoneNumberValid phoneNumber =
    let phoneNumberUtil = PhoneNumberUtil.GetInstance()

    phoneNumberUtil.Parse(phoneNumber, null)
    |> Option.ofObj
    |> Option.filter (fun number -> phoneNumberUtil.IsValidNumber number)
    |> Option.map (fun number ->
        match phoneNumberUtil.GetNumberType number with
        | PhoneNumberType.MOBILE -> true
        | _ -> false
    )
    |> Option.defaultValue false