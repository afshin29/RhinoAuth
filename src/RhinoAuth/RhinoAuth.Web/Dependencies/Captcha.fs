module Dependencies.Capthca

open FsToolkit.ErrorHandling
open Giraffe
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.Hosting


let private devCaptchaValidator _ _ = true |> TaskResult.ok


// Example options:
// - hCaptcha
// - reCAPTCHA
let private productionCaptchaValidator httpClient apiKey ipAddress token = raise <| System.NotImplementedException()


let getCaptchaValidator (ctx: HttpContext) =
    match ctx.GetWebHostEnvironment().IsDevelopment() with
    | true -> devCaptchaValidator
    | _ -> productionCaptchaValidator "get http client using ctx" "get api key using ctx"