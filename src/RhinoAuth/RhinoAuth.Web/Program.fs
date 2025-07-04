open Giraffe.EndpointRouting
open Microsoft.AspNetCore.Builder
open Microsoft.Extensions.Hosting

open Configurations


[<EntryPoint>]
let main args =
    let builder = WebApplication.CreateBuilder(args)

    builder
    |> addProjectConfigurations
    |> addProjectDatabase
    |> addProjectDataProtection
    |> addProjectHealthCheck
    |> addProjectServices
    |> addProjectAuth
    |> addProjectHostings
    |> ignore

    let webApp = builder.Build()

    webApp
        .UseDatabaseMigrator()
        .UseStaticFiles()
        .UseRouting()
        .UseDarkModeDetection()
        .UseAuthentication()
        .UseAuthorization()
        .UseEndpoints(_.MapGiraffeEndpoints(Routing.endpoints))
        .UseEndpoints(_.MapGiraffeEndpoints(Routing.oauthEndpoints))
        |> ignore

    webApp.Run()

    0 // Exit code

