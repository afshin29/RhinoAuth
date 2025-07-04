[<AutoOpen>]
module Extensions

open FsToolkit.ErrorHandling
open Giraffe
open Microsoft.AspNetCore.Antiforgery
open Microsoft.AspNetCore.Http
open Microsoft.IdentityModel.JsonWebTokens
open System.Security.Claims


[<Literal>]
let DarkThemeKey = "dark_theme"


type HttpRequest with

    member request.IsDarkMode() =
        match request.Cookies[DarkThemeKey] with
        | null ->
            match request.Headers.TryGetValue "Sec-CH-Prefers-Color-Scheme" with
            | true, value -> value.ToString() = "dark"
            | _ -> false
        | "1" -> true
        | _ -> false



type HttpContext with

    member ctx.GetCurrentIpAddress() =
        match ctx.Connection.RemoteIpAddress with
        | null -> System.Net.IPAddress.Loopback
        | ip when ip.IsIPv4MappedToIPv6 -> ip.MapToIPv4()
        | ip -> ip

    member ctx.GetAntiforgeryToken() =
        let afService = ctx.GetService<IAntiforgery>()
        let afTokenSet = afService.GetAndStoreTokens(ctx)
        afTokenSet.RequestToken



type ClaimsPrincipal with

    member user.IsLoggedIn() =
        match user.Identity with
        | NonNull id -> id.IsAuthenticated
        | _ -> false

    member user.GetId() =
        user.FindFirstValue(JwtRegisteredClaimNames.Sub)
        |> Option.ofObj
        |> Option.map int64

    member user.GetRequiredId() =
        user.GetId()
        |> Option.defaultWith (fun _ -> failwith "User ID is not available for current request")

    member user.GetRequiredUsername() =
        user.FindFirstValue(JwtRegisteredClaimNames.UniqueName)
        |> Option.ofObj
        |> Option.defaultWith (fun _ -> failwith "Username is not available for current request")

    member user.GetRequiredSessionId() =
        user.FindFirstValue(JwtRegisteredClaimNames.Sid)
        |> Option.ofObj
        |> Option.defaultWith (fun _ -> failwith "Session ID is not available for current request")

    member user.GetRoles() =
        user.FindAll("roles")
        |> Seq.map _.Value