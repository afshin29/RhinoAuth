namespace Dependencies

open FsToolkit.ErrorHandling
open Giraffe
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.Hosting


type FileManager = {
    SaveProfilePic: (string -> byte array -> TaskResult<unit, string>)
    DeleteProfilePic: (string -> TaskResult<unit, string>)
}


module FileManager =

    let private devFileManager wwwRootPath : FileManager = {
        SaveProfilePic =
            fun fileName content -> task {
                try
                    let folder = $"{wwwRootPath}/img/profile-pics"
                    System.IO.Directory.CreateDirectory folder |> ignore

                    do! System.IO.File.WriteAllBytesAsync($"{folder}/{fileName}.jpg", content)
                
                    return Ok ()
                with ex ->
                    return Error ex.Message
            }


        DeleteProfilePic =
            fun fileName ->
                try
                    Some $"{wwwRootPath}/img/profile-pics/{fileName}.jpg"
                    |> Option.bind (fun path -> if System.IO.File.Exists path then Some path else None)
                    |> Option.iter System.IO.File.Delete

                    TaskResult.ok ()
                with ex ->
                    TaskResult.error ex.Message
    }


    // Example options:
    // - Azure Blob Storage
    // - DigitalOcean Spaces
    let private productionFileManager httpClient apiKey : FileManager = raise <| System.NotImplementedException()


    let getFileManager (ctx: HttpContext) =
        let env = ctx.GetWebHostEnvironment()
        match env.IsDevelopment() with
        | true -> devFileManager env.WebRootPath
        | _ -> productionFileManager "get http client using ctx" "get api key using ctx"