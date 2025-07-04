module Utilities.KeyGenerator

open System.Security.Cryptography

let private alphaNumeric = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890".ToCharArray()

let private digits = "1234567890".ToCharArray()

let private humanReadable = "ABCDEFGHJKLMNPQRSTUVWXYZ123456789".ToCharArray()

let private generate size (chars: char array) =
    let data = Array.zeroCreate<byte> (4 * size)
    
    use crypto = RandomNumberGenerator.Create()
    crypto.GetBytes(data)

    let result = System.Text.StringBuilder(size)

    for i in 0 .. size - 1 do
        let rnd = System.BitConverter.ToUInt32(data, i * 4)
        let idx = int (rnd % uint32 chars.Length)
        result.Append(chars[idx]) |> ignore

    result.ToString()


let private getReadable size = generate size humanReadable

let private getNumeric size = generate size digits

let private getString size = generate size alphaNumeric

let private getStringRange minSize maxSize =
    let size = RandomNumberGenerator.GetInt32(minSize, maxSize)
    getString size



let getNumeric4Digits () = getNumeric 4
let getNumeric6Digits () = getNumeric 6
let getNumeric8Digits () = getNumeric 8


let getString32To64Chars () = getStringRange 32 65