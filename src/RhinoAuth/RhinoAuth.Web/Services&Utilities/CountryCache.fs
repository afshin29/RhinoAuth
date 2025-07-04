namespace Services

open RhinoAuth.Database
open Microsoft.EntityFrameworkCore
open Microsoft.Extensions.DependencyInjection
open System
open System.Linq
open System.Collections.Frozen


type CountryCache(serviceProvider: IServiceProvider) =

    let dbFetcher (sp: IServiceProvider) =
        use scope = sp.CreateScope()
        let dbContext = scope.ServiceProvider.GetRequiredService<RhinoDbContext>()
        dbContext.Countries
            .AsNoTracking()
            .OrderBy(_.Name)
            .ToFrozenDictionary(_.Code, id)

    let mutable countries = dbFetcher serviceProvider

    member this.Countries: FrozenDictionary<string, Country> = countries

    member this.update () =
        countries <- dbFetcher serviceProvider
