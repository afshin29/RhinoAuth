module WorkflowBuilder

open FsToolkit.ErrorHandling
open System.Net
open System.Security.Claims
open System.Threading.Tasks


type Dependencies = {
    IpInfoProvider: (IPAddress -> TaskResult<Dependencies.IpInfo.IpAddrInfo, string>)
    CaptchaValidator: (IPAddress -> string -> TaskResult<bool, string>)
    EmailSender: (Dependencies.EmailSender.EmailReason -> string -> TaskResult<unit, string>)
    SmsSender: (string -> TaskResult<unit, string>)
    FileManager: Dependencies.FileManager
    DistributedCache: Dependencies.DistributedCache
}

// We provide these data to our workflows so that they would not
// depend on framework types such as HttpContext, IServiceProvider, DbContext, etc.
type WorkflowData = {
    DependencyProvider: Dependencies
    AppLimits: Settings.AppLimits
    AppBehavior: Settings.AppBehavior
    DbCommands: Repository.Commands
    DbQueries: Repository.Queries
    HeaderProvider: (string -> string option)
    User: ClaimsPrincipal
    IpAddress: IPAddress
    IsDevEnv: bool
}


type WorkflowError =
    | FormValidationError of string
    | ResourceNotFound
    | BusinessRuleError of string
    | ExternalServiceError of string
    | ConcurrencyError


// Some helper methods to facilitate building workflows


let someOk a = Some <| Ok a
let someError a = Some <| Error a


let toTask f a =
    let result = f a
    Task.singleton result


let private tryPickTask (tasks: seq<Task<Result<unit, WorkflowError> option>>) : TaskResult<unit, WorkflowError> = task {
    use enumerator = tasks.GetEnumerator()
    let mutable found = Ok ()

    while enumerator.MoveNext() && found.IsOk do
        let! result = enumerator.Current
        match result with
        | Some r -> found <- r
        | None -> ()

    return found
}


// DB duplicate index errors are business rule errors
// and must be handled by workflow processor, otherwise we will fail
// If DB command does not have duplicate index problem, then it's safe to use this

let dbErrorMapper = function
    | Repository.ConcurrentDataAccess -> ConcurrencyError
    | Repository.DuplicateIndex idx -> failwith $"Failed to write to database because of duplicate index [{idx}]"

let mapDefaultDbError taskResult = taskResult |> TaskResult.mapError dbErrorMapper

let mapDbOptionToResult taskOption =
    taskOption
    |> Task.map (function
        | Some dbData -> Ok dbData
        | None -> Error ResourceNotFound
    )


let mapDependencyError taskResult = taskResult |> TaskResult.mapError ExternalServiceError


let private getFormValidationResult rules formModel =
    match rules |> Seq.tryPick (fun f -> f formModel) with
    | Some err -> Error <| FormValidationError err
    | None -> Ok ()
    |> Task.singleton


let private getPrefetchValidationResult rules formModel =
    rules
    |> Seq.map (fun f -> f formModel)
    |> tryPickTask


let private getBusinessRuleValidationResult rules formModel dbData =
    rules
    |> Seq.tryPick (fun f -> f formModel dbData)
    |> Option.defaultValue (Ok dbData)
    |> Task.singleton


// Every workflow has these steps:
//
// Step 1: Form-level validation
// Step 2: Prefetch async validation
// Step 3: Fetch data
// Step 4: Postfetch business rule validation
// Step 5: Process

let build formValidations prefetchValidations dbQuery businessRules processor workflowData formModel =
    formModel
    |> getFormValidationResult formValidations
    |> TaskResult.bind (fun _ -> getPrefetchValidationResult prefetchValidations formModel)
    |> TaskResult.bind (fun _ -> dbQuery ())
    |> TaskResult.bind (getBusinessRuleValidationResult businessRules formModel)
    |> TaskResult.bind (processor workflowData formModel)