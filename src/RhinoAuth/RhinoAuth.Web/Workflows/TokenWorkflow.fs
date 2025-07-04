module Workflows.TokenWorkflow

open FsToolkit.ErrorHandling
open RhinoAuth.Database
open System
open Utilities

open Repository
open WorkflowBuilder


type private GrantType =
    | AuthorizationCode
    | ClientCredentials
    | RefreshToken


let inline private detectGrantType (formModel: OAuthModels.TokenRequest) =
    match formModel.grant_type with
    | value when value.ToLower() = "authorization_code" -> Ok AuthorizationCode
    | value when value.ToLower() = "client_credentials" -> Ok ClientCredentials
    | value when value.ToLower() = "refresh_token" -> Ok RefreshToken
    | _ -> Error (OAuthModels.ProtocolError.unsupported_grant_type, "Invalid or unsupported grant_type")


let entry wd (formModel: OAuthModels.TokenRequest) =
    formModel
    |> detectGrantType
    |> TaskResult.ofResult
    |> TaskResult.bind (function
        | AuthorizationCode -> AuthorizationCodeWorkflow.entry wd formModel
        | ClientCredentials -> ClientCredentialsWorkflow.entry wd formModel
        | RefreshToken -> RefreshTokenWorkflow.entry wd formModel
    )