module Dependencies.IpInfo

open FsToolkit.ErrorHandling
open Giraffe
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.Hosting


type IpAddrInfo = {
    CountryCode: string
    IsUnsafeNetwork: bool
}


let private devIpInfoProvider _ = { CountryCode = "US"; IsUnsafeNetwork = false } |> TaskResult.ok


// Example options:
// - proxycheck.io
// - ipinfo.io
// - combine external service with local cache to reduce cost
let private productionIpInfoProvider httpClient apiKey ipAddress = raise <| System.NotImplementedException()


let getIpInfoProvider (ctx: HttpContext) =
    match ctx.GetWebHostEnvironment().IsDevelopment() with
    | true -> devIpInfoProvider
    | _ -> productionIpInfoProvider "get http client using ctx" "get api key using ctx"