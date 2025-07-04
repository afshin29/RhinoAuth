module Repository

open FsToolkit.ErrorHandling
open RhinoAuth.Database
open System.Threading.Tasks


// EF Core itself is a repository, the reason we have this is
// 1) so that we can try other libraries such as SqlHydra or SqlFun
// 2) our workflows would not need to depend on DbContext


type DbCommandError =
    | ConcurrentDataAccess
    | DuplicateIndex of string


type SignupDbData = {
    PhoneCountry: Country option
    IpCountry: Country option
    UsernameTaken: bool
    PhoneNumberTaken: bool
    EmailTaken: bool
    IpRequestCount: int
    PhoneNumberRequestCount: int
    TotalRequestCount: int
}

type ChangePhoneNumberDbData = {
    User: User
    PhoneCountry: Country option
    PhoneNumberTaken: bool
}

type ChangeEmailDbData = {
    User: User
    EmailTaken: bool
}


type OAuthAuthorizeDbData = {
    ApiClient: ApiClient option
    UnfinishedRequestsCount: int
}


type RefreshTokenDbData =
    | ForClientCredentials of ApiClientTokenRequest
    | ForOpenId of ExternalLogin


type Queries = {
    GetSignupData: (System.Net.IPAddress -> FormModels.SignupFM -> string -> Task<SignupDbData>)

    GetSignupRequest: (string -> Task<SignupRequest option>)

    GetUser: (int64 -> Task<User option>)

    GetUserIncludingCountry: (string -> Task<User option>)

    GetUserIncludingOneTimeCodes: (string -> Task<User option>)

    GetOneTimeCode: (string -> Task<OneTimeCode option>)

    GetChangePhoneNumberData: (int64 -> int -> string -> Task<ChangePhoneNumberDbData>)

    GetChangeEmailData: (int64 -> string -> Task<ChangeEmailDbData>)

    GetProfileVM: (int64 -> Task<ViewModels.ProfileVM>)

    GetAuthorizeData: (string -> int64 -> Task<OAuthAuthorizeDbData>)

    GetAuthorizeRequest: (string -> int64 -> Task<AuthorizeRequest option>)

    GetAuthorizeConsentVM: (string -> int64 -> Task<OAuthModels.ConsentVM option>)

    GetJWK: (unit -> Task<AppJsonWebKey>)

    GetApiClient: (string -> string -> Task<ApiClient option>)

    GetRefreshTokenData: (string -> string -> Task<RefreshTokenDbData option>)

    GetExternalLogin: (string -> Task<ExternalLogin option>)

    GetLogin: (string -> Task<Login option>)

    GetPublicKeys: (unit -> Task<OAuthModels.PublicKeyVM array>)
}



type Commands = {

    // EF Core does not need the entity to update it, because it tracks it internally
    // this parameter exists so that other libraries without entity tracking have a chance to update the row
    UpdateRow: (obj -> TaskResult<int, DbCommandError>)

    CreateRow: (obj -> TaskResult<int, DbCommandError>)

    CreateUser: (SignupRequest -> User -> TaskResult<int, DbCommandError>)
}