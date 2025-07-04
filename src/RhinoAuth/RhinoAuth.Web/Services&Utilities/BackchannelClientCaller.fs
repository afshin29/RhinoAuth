namespace Services

open Microsoft.AspNetCore.Hosting
open Microsoft.EntityFrameworkCore
open Microsoft.Extensions.Configuration
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Hosting
open RhinoAuth.Database
open System
open System.Collections.Concurrent
open System.Collections.Generic
open System.Linq
open System.Net.Http
open System.Threading
open System.Threading.Tasks


type BackchannelClientCaller(scf: IServiceScopeFactory) =
    inherit BackgroundService()

    let httpClient = new HttpClient()

    let mutable key: AppJsonWebKey = Unchecked.defaultof<_>
    let mutable lastFetchTime = DateTimeOffset.MinValue

    static member val ExternalLoginIds = new BlockingCollection<string>(1000) with get

    member private _.getKey(dbContext: RhinoDbContext) =
        if lastFetchTime.AddMinutes(5) < DateTimeOffset.UtcNow then
            key <-
                dbContext.AppJsonWebKeys
                    .AsNoTracking()
                    .Where(fun x -> x.IsActive)
                    .OrderByDescending(fun x -> x.CreatedAt)
                    .First()
            lastFetchTime <- DateTimeOffset.UtcNow

        key

    override this.ExecuteAsync(stoppingToken: CancellationToken) : Task =
        task {
            do! Task.Yield()

            while not stoppingToken.IsCancellationRequested do
                let id = BackchannelClientCaller.ExternalLoginIds.Take(stoppingToken)

                use scope = scf.CreateScope()
                let serviceProvider = scope.ServiceProvider
                let dbContext = serviceProvider.GetRequiredService<RhinoDbContext>()
                let issuer = serviceProvider.GetRequiredService<IConfiguration>().GetValue<string>("AppBehavior:OAuthIssuer")

                let externalLogin =
                    dbContext.ExternalLogins
                        .AsNoTracking()
                        .Include(_.ApiClient)
                        .FirstOrDefault(fun x -> x.Id = id)

                if isNull externalLogin then
                    () // skip to next iteration
                else
                    let logoutToken =
                        Utilities.TokenGenerator.getLogoutToken 
                            externalLogin.UserId
                            externalLogin.LoginId
                            issuer
                            (externalLogin.ExternalLoginApiResources |> Seq.map _.ApiResourceId |> Seq.toArray)
                            (this.getKey dbContext) 

                    let mutable address = externalLogin.ApiClient.BackchannelLogoutUri

                    let content = new FormUrlEncodedContent([ KeyValuePair("logout_token", logoutToken) ])
                        
                    let! response = httpClient.PostAsync(address, content, stoppingToken)

                    let! responseBody = response.Content.ReadAsStringAsync()

                    let callLog =
                        ApiClientHttpCall(
                            Id = Utilities.KeyGenerator.getString32To64Chars(),
                            Address = address,
                            ApiClientId = externalLogin.ApiClientId,
                            ExternalLoginId = externalLogin.Id,
                            Payload = logoutToken,
                            ResponseCode = int response.StatusCode,
                            ResponseBody = responseBody
                        )

                    dbContext.Add(callLog) |> ignore
                    dbContext.SaveChanges() |> ignore
        }