namespace Dependencies

open FsToolkit.ErrorHandling
open Giraffe
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.Configuration
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.Logging
open StackExchange.Redis
open System
open System.Collections.Generic


type DistributedCache = {
    // key -> value -> expiry
    StringSet: (string -> string -> TimeSpan -> unit)
    StringGet: (string -> string option)
}


module DistributedCache =

    let private devCache = Dictionary<string, string>()

    let private inMemoryDev : DistributedCache = {

        StringSet =
            fun key value _ ->
                devCache[key] = value |> ignore

        StringGet =
            fun key ->
                match devCache.ContainsKey key with
                | false -> None
                | _ -> Some <| devCache[key]
    }


    let private redis (serverConnection: ConnectionMultiplexer) : DistributedCache = {
        
        StringSet =
            fun key value expiry ->
                let cacheDb = serverConnection.GetDatabase()
                cacheDb.StringSet(key, value, expiry) |> ignore

        StringGet =
            fun key ->
                let cacheDb = serverConnection.GetDatabase()
                match cacheDb.StringGet key with
                | value when value = RedisValue.Null -> None
                | value -> Some <| value.ToString()
    }


    let getDistributedCache (ctx: HttpContext) = inMemoryDev