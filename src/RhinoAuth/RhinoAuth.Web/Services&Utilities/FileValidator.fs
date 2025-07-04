module Utilities.FileValidator

open FileTypeInterrogator


let private fileInterrogator = FileTypeInterrogator()

let private isValidMimeType mimeType (file: byte array) =

    let detectedMime =
        fileInterrogator.DetectType(file)
        |> Option.ofObj
        |> Option.bind (fun t -> Option.ofObj t.MimeType)
        |> Option.map _.ToLower()
        |> Option.defaultValue ""

    detectedMime = mimeType


let isValidJpegImage = isValidMimeType "image/jpeg"
