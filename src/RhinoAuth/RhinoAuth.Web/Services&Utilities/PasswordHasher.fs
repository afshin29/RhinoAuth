module Utilities.PasswordHasher

open Microsoft.AspNetCore.Cryptography.KeyDerivation
open System
open System.Security.Cryptography

[<Literal>]
let private iterationCount = 10_000;

[<Literal>]
let private saltSize = 16;

[<Literal>]
let private requestedBytes = 32;

let private prf = KeyDerivationPrf.HMACSHA256
let private rng = RandomNumberGenerator.Create()


let private readNetworkByteOrder (buffer: byte[]) (offset: int) : uint =
    (uint buffer[offset + 0] <<< 24)
    ||| (uint buffer[offset + 1] <<< 16)
    ||| (uint buffer[offset + 2] <<< 8) 
    ||| (uint buffer[offset + 3])

let private writeNetworkByteOrder (buffer: byte[]) (offset: int) (value: uint) : unit =
    buffer[offset + 0] <- byte (value >>> 24)
    buffer[offset + 1] <- byte (value >>> 16)
    buffer[offset + 2] <- byte (value >>> 8)
    buffer[offset + 3] <- byte value


let hashPassword password =
    let salt = Array.zeroCreate<byte> saltSize
    rng.GetBytes salt

    let subKey = KeyDerivation.Pbkdf2(password, salt, prf, iterationCount, requestedBytes)

    let outputBytes = Array.zeroCreate<byte> (13 + salt.Length + subKey.Length)
    outputBytes[0] <- 0x01uy

    writeNetworkByteOrder outputBytes 1 (uint prf)
    writeNetworkByteOrder outputBytes 5 (uint iterationCount)
    writeNetworkByteOrder outputBytes 9 (uint saltSize)

    Buffer.BlockCopy(salt, 0, outputBytes, 13, saltSize)
    Buffer.BlockCopy(subKey, 0, outputBytes, 13 + saltSize, subKey.Length)

    Convert.ToBase64String outputBytes


let isValidHashedPassword hash password =
    let decodedHashedPassword = Convert.FromBase64String hash

    try

        let prf = enum<KeyDerivationPrf> <| int (readNetworkByteOrder decodedHashedPassword 1)
        let iterCount = int <| readNetworkByteOrder decodedHashedPassword 5
        let saltLength = int <| readNetworkByteOrder decodedHashedPassword 9

        if saltLength < 128 / 8 then
            false
        else

            let salt = Array.zeroCreate<byte> saltSize
            Buffer.BlockCopy(decodedHashedPassword, 13, salt, 0, salt.Length)
            
            let subkeyLength = decodedHashedPassword.Length - 13 - salt.Length
            if subkeyLength < 128 / 8 then
                false
            else
            
                let expectedSubKey = Array.zeroCreate<byte> subkeyLength
                Buffer.BlockCopy(decodedHashedPassword, 13 + salt.Length, expectedSubKey, 0, expectedSubKey.Length)
                
                let actualSubKey = KeyDerivation.Pbkdf2(password, salt, prf, iterCount, subkeyLength)
                CryptographicOperations.FixedTimeEquals(actualSubKey, expectedSubKey)

    with
    | _ -> false