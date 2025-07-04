module Dependencies.EmailSender

open FsToolkit.ErrorHandling
open Giraffe
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.Logging


type EmailReason =
    | Verification
    | Alert of string


let private getEmailTitle reason =
    match reason with
    | Verification -> "Email Verification"
    | Alert text -> $"Alert: {text}"


let private getEmailTemplate reason text =
    match reason with
    | Verification -> $"Your verification code is: {text}"
    | Alert _ -> text


let private devEmailSender (logger: ILogger) reason text =
    let (title, body) = getEmailTitle reason, getEmailTemplate reason text
    logger.LogInformation($"[{title}] {body}")
    |> TaskResult.ok


// Example options:
// - Mailchimp
// - SendGrid
let private productionEmailSender httpClient apiKey reason text = raise <| System.NotImplementedException()


let getEmailSender (ctx: HttpContext) =
    match ctx.GetWebHostEnvironment().IsDevelopment() with
    | true -> devEmailSender <| ctx.GetLogger()
    | _ -> productionEmailSender "get http client using ctx" "get api key using ctx"