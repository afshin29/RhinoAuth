module Dependencies.SmsSender

open FsToolkit.ErrorHandling
open Giraffe
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.Logging


let private devSmsSender (logger: ILogger) text =
    logger.LogInformation($"[SMS] {text}")
    |> TaskResult.ok


// Example options:
// - Plivo
// - Twilio
let private productionSmsSender httpClient apiKey text = raise <| System.NotImplementedException()


let getSmsSender (ctx: HttpContext) =
    match ctx.GetWebHostEnvironment().IsDevelopment() with
    | true -> devSmsSender <| ctx.GetLogger()
    | _ -> productionSmsSender "get http client using ctx" "get api key using ctx"