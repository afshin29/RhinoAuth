module Configurations

open FsToolkit.ErrorHandling
open Giraffe
open Microsoft.AspNetCore.Authentication.Cookies
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.DataProtection
open Microsoft.AspNetCore.Http
open Microsoft.AspNetCore.HttpOverrides
open Microsoft.EntityFrameworkCore
open Microsoft.Extensions.Configuration
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Hosting
open Microsoft.IdentityModel.JsonWebTokens
open RhinoAuth.Database
open StackExchange.Redis
open System
open System.Text.Json
open System.Text.Json.Serialization
open System.Reflection
open System.Security.Claims


// Builder function

let addProjectConfigurations (builder: WebApplicationBuilder) =
    builder.Services
        .Configure<ForwardedHeadersOptions>(fun (options: ForwardedHeadersOptions) ->
            options.ForwardedHeaders <-
                ForwardedHeaders.XForwardedFor
                ||| ForwardedHeaders.XForwardedProto
                ||| ForwardedHeaders.XForwardedHost
        
            options.KnownNetworks.Clear()
            options.KnownProxies.Clear()
        )
        .Configure<Settings.AppLimits>(builder.Configuration.GetSection <| nameof Settings.AppLimits)
        .Configure<Settings.AppBehavior>(builder.Configuration.GetSection <| nameof Settings.AppBehavior)
        .ConfigureHttpJsonOptions(fun options ->
            options.SerializerOptions.PropertyNamingPolicy <- JsonNamingPolicy.SnakeCaseLower
            options.SerializerOptions.DefaultIgnoreCondition <- JsonIgnoreCondition.WhenWritingNull
        )
        |> ignore

    builder


let addProjectDatabase (builder: WebApplicationBuilder) =
    let connectionString =
        builder.Configuration.GetConnectionString "Default"
        |> Option.ofObj
        |> Option.defaultWith (fun _ -> failwith "Connection string is required")

    let migrationAssembly = Assembly.GetAssembly(typeof<RhinoDbContext>).FullName
    builder.Services.AddDbContextPool<RhinoDbContext>(fun options ->
        options
            .UseNpgsql(
                connectionString,

                (fun npgBuilder ->
                    npgBuilder.EnableRetryOnFailure() |> ignore
                    npgBuilder.MigrationsAssembly(migrationAssembly) |> ignore
                )
            )
            .UseSnakeCaseNamingConvention()
            .UseSeeding(RhinoDbContext.SeederFunc)
            |> ignore
    ) |> ignore

    builder


let addProjectDataProtection (builder: WebApplicationBuilder) =
    builder.Services
        .AddDataProtection()
        .PersistKeysToDbContext<RhinoDbContext>()
        |> ignore

    builder


let addProjectHealthCheck (builder: WebApplicationBuilder) =
    builder.Services
        .AddHealthChecks()
        .AddDbContextCheck<RhinoDbContext>("db", tags = [ "ready" ])
        |> ignore

    builder


let addProjectServices (builder: WebApplicationBuilder) =
    builder.Services
        .AddSingleton<Services.CountryCache>()
        .AddSingleton<ConnectionMultiplexer>(fun sp ->
            let configuration = sp.GetRequiredService<IConfiguration>()
            let address = $"""{configuration["Redis:Host"]}:{configuration["Redis:Port"]}"""
            ConnectionMultiplexer.Connect(address)
        )
        .AddHostedService<Services.BackchannelClientCaller>()
        .AddAntiforgery(fun options ->
            options.FormFieldName <- View.ViewConstants.AntiforgeryInputName
            options.HeaderName <- null
        )
        .AddGiraffe()
        |> ignore

    builder


let addProjectAuth (builder: WebApplicationBuilder) =
    builder.Services
        .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
        .AddCookie(fun options ->
            options.ExpireTimeSpan <- TimeSpan.FromDays 30
            options.SlidingExpiration <- true

            options.Cookie.Name <- "auth_rhino"
            options.Cookie.HttpOnly <- true
            options.Cookie.SameSite <- SameSiteMode.Lax
            options.Cookie.SecurePolicy <- CookieSecurePolicy.Always
            
            options.LoginPath <- "/login"
            options.AccessDeniedPath <- "/forbidden"

            options.ReturnUrlParameter <- "returnUrl"

            options.Events.OnCheckSlidingExpiration <- (fun cookieContext ->
                task {
                    let dbContext = cookieContext.HttpContext.RequestServices.GetRequiredService<RhinoDbContext>()

                    let getSidClaim (p: ClaimsPrincipal) = p.FindFirstValue JwtRegisteredClaimNames.Sid |> Option.ofObj
                    
                    let getLogin id = dbContext.Logins.FirstOrDefaultAsync(fun x -> x.Id = id)
                    
                    let updateLogin (login: Login) =
                        login.UpdatedAt <- DateTimeOffset.UtcNow
                        dbContext.SaveChangesAsync()

                    do! cookieContext.Principal
                        |> Option.ofObj
                        |> Option.bind getSidClaim
                        |> Option.map getLogin
                        |> Option.sequenceTask
                        |> TaskOption.bind (updateLogin >> Task.map Some)
                        |> Task.ignore
                })
        )
        |> ignore

    builder.Services.AddAuthorization() |> ignore

    builder


let addProjectHostings (builder: WebApplicationBuilder) =
    builder



// Pipeline extensions

type WebApplication with

    member app.UseDatabaseMigrator() =
        if app.Environment.IsDevelopment() then
            use scope = app.Services.CreateScope()
            let dbContext = scope.ServiceProvider.GetRequiredService<RhinoDbContext>()
            dbContext.Database.Migrate()
        
        app



type IApplicationBuilder with

    member app.UseDarkModeDetection() =
        app.Use (fun (ctx: HttpContext) (next: RequestDelegate) ->
            ctx.Response.Headers.Append("Accept-CH", "Sec-CH-Prefers-Color-Scheme")
            ctx.Response.Headers.Append("Vary", "Sec-CH-Prefers-Color-Scheme")
            ctx.Response.Headers.Append("Critical-CH", "Sec-CH-Prefers-Color-Scheme")

            next.Invoke ctx
        ) |> ignore

        app

