[<RequireQualifiedAccess>]
module OAuthModels


[<Literal>]
let expirationInMinutes = 5


[<RequireQualifiedAccess>]
type ProtocolError =

    // OAuth
    | invalid_request
    | invalid_client
    | invalid_grant
    | unauthorized_client
    | access_denied
    | unsupported_grant_type
    | unsupported_response_type
    | invalid_scope
    | invalid_target
    | server_error
    | temporarily_unavailable

    // OIDC
    | interaction_required
    | login_required
    | account_selection_required
    | consent_required
    | invalid_request_object
    | request_not_supported
    | request_uri_not_supported
    | registration_not_supported



[<CLIMutable>]
type AuthorizeRequest = {
    response_type: string
    
    client_id: string
    
    redirect_uri: string option
    
    code_challenge: string
    
    code_challenge_method: string option
    
    state: string option
    
    scope: string option

    resource: string list option

    nonce: string option

    prompt: string option
}


[<CLIMutable>]
type ConsentFM = {
    Code: string
}


type ConsentVM = {
    Code: string
    ClientName: string
    ClientLogo: string option
    Scopes: string array
    Resources: string array
}


[<CLIMutable>]
type TokenRequest = {
    grant_type: string

    client_id: string
    
    client_secret: string option
    
    code: string option
    
    code_verifier: string option
    
    refresh_token: string option
    
    scope: string option
    
    resource: string list option
}


type TokenErrorResponse = {
    error: string

    error_description: string
}


type TokenSuccessResponse = {
    access_token: string | null
    
    id_token: string | null
    
    refresh_token: string

    expires_in: int64

    is_persistent: bool

    token_type: string
}


[<CLIMutable>]
type LogoutFM = {
    post_logout_redirect_uri: string option
    
    id_token_hint: string option
    
    client_id: string option
    
    state: string option
}


[<CLIMutable>]
type PublicKeyVM = {
    kty: string

    crv: string

    x: string

    y: string
}