module Utilities.TokenGenerator

open Microsoft.IdentityModel.Tokens
open RhinoAuth.Database
open System
open System.Collections.Generic
open System.IdentityModel.Tokens.Jwt
open System.Security.Claims
open System.Security.Cryptography
open System.Text


let private rnd = RandomNumberGenerator.Create()



let private getJwkSigningKey (jwk: AppJsonWebKey) =
    
    let ecParams = ECParameters(
        Curve = ECCurve.NamedCurves.nistP256,
        D = Base64UrlEncoder.DecodeBytes(jwk.D),
        Q = ECPoint(
            X = Base64UrlEncoder.DecodeBytes(jwk.X),
            Y = Base64UrlEncoder.DecodeBytes(jwk.Y)
        )
    )

    SigningCredentials(
        ECDsaSecurityKey(ECDsa.Create ecParams),
        SecurityAlgorithms.EcdsaSha256
    )



let private getProfileClaims (user: User) scopes =
    [
        if List.contains "profile" scopes then
            Claim(JwtRegisteredClaimNames.GivenName, user.FirstName)
            Claim(JwtRegisteredClaimNames.FamilyName, user.LastName)
            Claim(JwtRegisteredClaimNames.UniqueName, user.Username)
            
            if user.Avatar |> Option.ofObj |> Option.isSome then
                // What about the domain part?
                Claim(JwtRegisteredClaimNames.Picture, $"/img/profile-pics/{user.Avatar}.jpg")


        if List.contains "email" scopes then
            Claim(JwtRegisteredClaimNames.Email, user.Email)

        if List.contains "phone" scopes then
            Claim(JwtRegisteredClaimNames.PhoneNumber, $"+{user.CountryPhoneCode}{user.PhoneNumber}")
    ]



let private getUserClaims (user: User) loginSessionId =
    [
        Claim(JwtRegisteredClaimNames.Jti, KeyGenerator.getString32To64Chars())
        Claim(JwtRegisteredClaimNames.Sid, loginSessionId)
        Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString())
    ] @
    (user.UserRoles
    |> Seq.map (fun userRole -> Claim("roles", userRole.RoleId))
    |> Seq.toList)



let private createToken (claims: Claim seq) issuer audiences signingCredentials =

    let tokenDescriptor = SecurityTokenDescriptor(
        Subject = ClaimsIdentity(claims),
        Issuer = issuer,
        IssuedAt = Nullable DateTime.UtcNow,
        Expires = Nullable (DateTime.UtcNow.AddHours 1),
        SigningCredentials = signingCredentials
    )

    if Array.length audiences = 1 then
        tokenDescriptor.Audience = audiences[0] |> ignore
    else
        tokenDescriptor.Claims.Add <| KeyValuePair(JwtRegisteredClaimNames.Aud, audiences)

    let tokenHandler = JwtSecurityTokenHandler()
    let token = tokenHandler.CreateToken tokenDescriptor

    tokenHandler.WriteToken token



let getSha256 (value: string) =
    let inputBytes = Encoding.ASCII.GetBytes(value)
    let inputHash = SHA256.HashData(inputBytes)
    Base64UrlEncoder.Encode(inputHash)



let getLogoutToken userId loginSessionId issuer audiences jwk =
    
    let signingCred = getJwkSigningKey(jwk)

    let eventsJson = """
    {
        "http://schemas.openid.net/event/backchannel-logout": {}
    }
    """

    let claims = [
        Claim(JwtRegisteredClaimNames.Jti, KeyGenerator.getString32To64Chars())
        Claim(JwtRegisteredClaimNames.Sid, loginSessionId)
        Claim(JwtRegisteredClaimNames.Sub, userId.ToString())
        Claim("events", eventsJson, JsonClaimValueTypes.Json)
    ]

    createToken claims issuer audiences signingCred



let getIdToken user loginSessionId issuer oidcScopes audiences nonce jwk =

    let signingCred = getJwkSigningKey jwk
    let claims =
        getUserClaims user loginSessionId
        @ getProfileClaims user oidcScopes
        @ [
            if Option.isSome nonce then
                Claim(JwtRegisteredClaimNames.Nonce, nonce.Value)
        ]

    createToken claims issuer audiences signingCred



let getAccessTokenOfUser user loginSessionId issuer audiences (sharedSecret: string option) jwk =

    let signingCred =
        match sharedSecret with
        | Some secret ->
            let key = Encoding.ASCII.GetBytes(secret)
            new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256)
        | None -> getJwkSigningKey jwk
    
    let claims = getUserClaims user loginSessionId
    createToken claims issuer audiences signingCred


let getAccessTokenOfClient (apiClient: ApiClient) issuer audiences (sharedSecret: string option) jwk =

    let signingCred =
        match sharedSecret with
        | Some secret ->
            let key = Encoding.ASCII.GetBytes(secret)
            new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256)
        | None -> getJwkSigningKey jwk

    let claims = [
        Claim(JwtRegisteredClaimNames.Jti, KeyGenerator.getString32To64Chars())
        Claim(JwtRegisteredClaimNames.Sub, apiClient.Id)
    ]

    createToken claims issuer audiences signingCred



let getRefreshToken () =
    let randomNumber = Array.zeroCreate<byte> 32
    rnd.GetBytes randomNumber
    Convert.ToBase64String randomNumber