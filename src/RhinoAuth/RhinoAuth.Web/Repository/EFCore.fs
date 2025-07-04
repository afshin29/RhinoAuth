module DbAccess.EFCore

open RhinoAuth.Database
open Microsoft.EntityFrameworkCore
open System.Collections.Generic
open System.Linq


let private getTrackingEntity<'a when 'a: not null and 'a: not struct and 'a :> BaseEntity> (dbContext: RhinoDbContext) id = task {

    let! entity = dbContext.Set<'a>().FirstOrDefaultAsync(fun x -> x.Id = id)
    
    return Option.ofObj entity
}


let private getTrackingEntityIncluding<'a when 'a: not null and 'a: not struct and 'a :> BaseEntity> (dbContext: RhinoDbContext) (including: 'a -> obj) id = task {

    let! entity =
        dbContext.Set<'a>()
            .Include(including)
            .FirstOrDefaultAsync(fun x -> x.Id = id)
    
    return Option.ofObj entity
}


let private saveTrackingEntity (dbContext: RhinoDbContext) = task {
    try
        let! changedRows = dbContext.SaveChangesAsync()
        return Ok changedRows
    with
    | :? DbUpdateConcurrencyException -> return Error Repository.ConcurrentDataAccess
}


[<CLIMutable>]
type private EF_SignupData = {
    PhoneCountry: Country | null
    IpCountry: Country | null
    UsernameTaken: bool
    PhoneNumberTaken: bool
    EmailTaken: bool
    IpRequestCount: int
    PhoneNumberRequestCount: int
    TotalRequestCount: int
}

[<CLIMutable>]
type private EF_ChangePhonemberData = {
    User: User
    PhoneCountry: Country | null
    PhoneNumberTaken: bool
}

[<CLIMutable>]
type private EF_ChangeEmailData = {
    User: User | null
    EmailTaken: bool
}


[<CLIMutable>]
type private EF_OAuthAuthorizeData = {
    ApiClient: ApiClient | null
    UnfinishedRequestsCount: int
}


[<CLIMutable>]
type private EF_RefreshTokenData = {
    TokenRequest: ApiClientTokenRequest | null
    ExternalLogin: ExternalLogin | null
}


let getQueries (dbContext: RhinoDbContext) : Repository.Queries = {

    GetSignupData =
        fun ipAddress formModel ipCountryCode -> task {
            let! data =
                dbContext.Countries
                    .Select(fun _ ->
                        {
                            PhoneCountry = dbContext.Countries.AsNoTracking().FirstOrDefault(fun c -> c.PhoneCode = formModel.PhoneCode)
                            IpCountry = dbContext.Countries.AsNoTracking().FirstOrDefault(fun c -> c.Code = ipCountryCode)
                            UsernameTaken = dbContext.Users.Any(fun x -> x.Username = formModel.Username.ToLower())
                            PhoneNumberTaken = dbContext.Users.Any(fun x -> x.CountryPhoneCode = formModel.PhoneCode && x.PhoneNumber = formModel.PhoneNumber)
                            EmailTaken = dbContext.Users.Any(fun x -> x.Email = formModel.Email.ToLower())
                            IpRequestCount = dbContext.SignupRequests.Count(fun x -> x.IpAddress = ipAddress)
                            PhoneNumberRequestCount = dbContext.SignupRequests.Count(fun x -> x.PhoneNumber = formModel.PhoneNumber)
                            TotalRequestCount = dbContext.SignupRequests.Count()
                        } : EF_SignupData
                    )
                    .FirstAsync()
            
            return ({
                PhoneCountry = Option.ofObj data.PhoneCountry
                IpCountry = Option.ofObj data.IpCountry
                UsernameTaken = data.UsernameTaken
                PhoneNumberTaken = data.PhoneNumberTaken
                EmailTaken = data.EmailTaken
                IpRequestCount = data.IpRequestCount
                PhoneNumberRequestCount = data.PhoneNumberRequestCount
                TotalRequestCount = data.TotalRequestCount
            } : Repository.SignupDbData)
        }


    GetSignupRequest = fun id -> getTrackingEntity<SignupRequest> dbContext id


    GetUser = fun id -> getTrackingEntity<User> dbContext id


    GetUserIncludingCountry =
        fun username -> task {
            let! user =
                dbContext.Users
                    .Include(_.Country)
                    .FirstOrDefaultAsync(fun u -> u.Username = username)

            return Option.ofObj user
        }


    GetUserIncludingOneTimeCodes =
        fun username -> task {
            let! user =
                dbContext.Users
                    .Include(_.OneTimeCodes)
                    .FirstOrDefaultAsync(fun u -> u.Username = username)

            return Option.ofObj user
        }


    GetOneTimeCode =
        fun id -> task {
            let! otc =
                dbContext.OneTimeCodes
                    .Include(_.User)
                    .FirstOrDefaultAsync(fun c -> c.Id = id)

            return Option.ofObj otc
        }


    GetChangePhoneNumberData =
        fun userId countryPhoneCode phoneNumber -> task {
            let! data =
                dbContext.Countries
                    .Select(fun _ ->
                        {
                            User = dbContext.Users.Include(_.OneTimeCodes).First(fun u -> u.Id = userId)
                            PhoneCountry = dbContext.Countries.AsNoTracking().FirstOrDefault(fun c -> c.PhoneCode = countryPhoneCode)
                            PhoneNumberTaken = dbContext.Users.Any(fun x -> x.CountryPhoneCode = countryPhoneCode && x.PhoneNumber = phoneNumber)
                        } : EF_ChangePhonemberData
                    )
                    .FirstAsync()

            return ({
                User = data.User
                PhoneCountry = Option.ofObj data.PhoneCountry
                PhoneNumberTaken = data.PhoneNumberTaken
            } : Repository.ChangePhoneNumberDbData)
        } 


    GetChangeEmailData =
        fun userId email -> task {
            let! data =
                dbContext.Countries
                    .Select(fun _ ->
                        {
                            User = dbContext.Users.Include(_.OneTimeCodes).First(fun u -> u.Id = userId)
                            EmailTaken = dbContext.Users.Any(fun x -> x.Email = email.ToLower())
                        } : EF_ChangeEmailData
                    )
                    .FirstAsync()

            return ({
                User = data.User
                EmailTaken = data.EmailTaken
            } : Repository.ChangeEmailDbData)
        } 


    GetProfileVM =
        fun id -> task {
            let! user =
                dbContext.Users
                    .Include(fun u -> u.Logins :> IEnumerable<_>)
                    .ThenInclude(fun (l: Login) -> l.ExternalLogins)
                    .FirstAsync(fun u -> u.Id = id)

            return ({
                Username = user.Username
                Email = user.Email
                CountryCode = user.CountryCode
                CountryPhoneCode = user.CountryPhoneCode
                PhoneNumber = user.PhoneNumber
                FirstName = user.FirstName
                LastName = user.LastName
                Avatar = Option.ofObj user.Avatar
                ActiveSessions = [||]
            }: ViewModels.ProfileVM)
        }


    GetAuthorizeData =
        fun clientId userId -> task {
            let! data =
                dbContext.ApiClients
                    .Select(fun _ ->
                        {
                            ApiClient =
                                dbContext.ApiClients
                                    .Include(fun c -> c.ApiClientResources :> IEnumerable<_>)
                                    .ThenInclude(fun (cr: ApiClientResource) -> cr.ApiResource)
                                    .FirstOrDefault(fun c -> c.Id = clientId)
                            UnfinishedRequestsCount = dbContext.AuthorizeRequests.Count(fun r ->
                                r.UserId = userId
                                && r.ApiClientId = clientId
                                && not r.CompletedAt.HasValue
                                && r.CreatedAt.AddHours 1 > System.DateTimeOffset.UtcNow
                            )
                        } : EF_OAuthAuthorizeData
                    )
                    .FirstOrDefaultAsync()

            return
                match data with
                | Null -> ({
                    ApiClient = None
                    UnfinishedRequestsCount = 0
                }: Repository.OAuthAuthorizeDbData)
                
                | NonNull d ->({
                    ApiClient = d.ApiClient |> Option.ofObj
                    UnfinishedRequestsCount = d.UnfinishedRequestsCount
                }: Repository.OAuthAuthorizeDbData)
        }


    GetAuthorizeRequest =
        fun code userId -> task {
            let! authorizeRequest =
                dbContext.AuthorizeRequests
                    .Include(_.ApiClient)
                    .Include(fun r -> r.AuthorizeRequestApiResources :> IEnumerable<_>)
                        .ThenInclude(fun (ar: AuthorizeRequestApiResource) -> ar.ApiResource)
                    .Include(_.Login)
                    .Include(_.User)
                        .ThenInclude(_.UserRoles)
                    .FirstOrDefaultAsync(fun r -> r.Id = code && r.UserId = userId)

            return Option.ofObj authorizeRequest
        }


    GetAuthorizeConsentVM =
        fun code userId -> task {
            let! authorizeRequest =
                dbContext.AuthorizeRequests
                    .Include(_.ApiClient)
                    .Include(_.AuthorizeRequestApiResources)
                    .FirstOrDefaultAsync(fun r -> r.Id = code && r.UserId = userId)

            return
                match authorizeRequest with
                | Null -> None
                
                | NonNull ar ->
                    Some ({
                        Code = ar.Id
                        ClientName = ar.ApiClient.DisplayName
                        ClientLogo = Option.ofObj ar.ApiClient.Logo
                        Scopes = ar.Scopes.ToArray()
                        Resources = (ar.AuthorizeRequestApiResources |> Seq.map _.ApiResourceId |> Seq.toArray)
                    }: OAuthModels.ConsentVM)
        }


    GetJWK =
        fun () -> task {
            let! jwk =
                dbContext.AppJsonWebKeys
                    .AsNoTracking()
                    .Where(fun key -> key.IsActive && key.CreatedAt.AddDays(60) > System.DateTimeOffset.UtcNow)
                    .OrderByDescending(_.CreatedAt)
                    .FirstAsync()

            return jwk
        }


    GetApiClient =
        fun id secret -> task {
            let! apiClient =
                dbContext.ApiClients
                    .Include(_.ApiClientResources)
                    .FirstOrDefaultAsync(fun c -> c.Id = id && c.Secret = secret)

            return Option.ofObj apiClient
        }


    GetRefreshTokenData =
        fun clientId refreshToken -> task {
            let! dbData =
                dbContext.ApiClients
                    .Select(fun _ ->
                        {
                            TokenRequest = dbContext.ApiClientTokenRequests.Include(_.ApiClient).FirstOrDefault(fun r -> r.ApiClientId = clientId && r.RefreshToken = refreshToken)
                            ExternalLogin =
                                dbContext.ExternalLogins
                                    .Include(_.ApiClient)
                                    .Include(_.Login)
                                        .ThenInclude(_.User)
                                        .ThenInclude(_.UserRoles)
                                    .FirstOrDefault(fun e ->
                                        e.ApiClientId = clientId
                                        && (e.RefreshToken = refreshToken || e.PreviousRefreshToken = refreshToken)
                                    )
                        }: EF_RefreshTokenData
                    )
                    .FirstAsync()

            return
                match (dbData.TokenRequest, dbData.ExternalLogin) with
                | (NonNull tokenRequest, _) -> Some <| Repository.ForClientCredentials tokenRequest
                | (_, NonNull externalLogin) -> Some <| Repository.ForOpenId externalLogin
                | _ -> None
        }


    GetExternalLogin =
        fun idToken -> task {
            let! externalLogin =
                dbContext.ExternalLogins
                    .Include(_.ApiClient)
                    .Include(_.Login)
                        .ThenInclude(_.User)
                        .ThenInclude(_.UserRoles)
                    .FirstOrDefaultAsync(fun e -> e.IdToken = idToken)

            return Option.ofObj externalLogin
        }


    GetLogin =
        fun id -> task {
            let! login =
                dbContext.Logins
                    .Include(fun l -> l.ExternalLogins :> IEnumerable<_>)
                        .ThenInclude(fun (el: ExternalLogin) -> el.ApiClient)
                    .FirstOrDefaultAsync(fun l -> l.Id = id)

            return Option.ofObj login
        }

    GetPublicKeys =
        fun () -> task {
            let maxValidDate = System.DateTimeOffset.UtcNow.AddDays 60
            let! keys = 
                dbContext.AppJsonWebKeys
                    .Where(fun x -> x.IsActive && x.CreatedAt < maxValidDate)
                    .Select(fun jwk ->
                        ({
                            kty = "EC"
                            crv = "P-256"
                            x = jwk.X
                            y = jwk.Y
                        }: OAuthModels.PublicKeyVM)
                    )
                    .ToArrayAsync()

            return keys
        }
}



let getCommands (dbContext: RhinoDbContext) : Repository.Commands = {

    UpdateRow = (fun _ -> saveTrackingEntity dbContext)


    CreateRow =
        fun row -> task {
            dbContext.Add(row) |> ignore
            return! saveTrackingEntity dbContext
        }


    CreateUser =
        fun signupRequest user -> task {
            dbContext.Remove(signupRequest) |> ignore
            dbContext.Add(user) |> ignore
            
            try
                let! changedRows = dbContext.SaveChangesAsync()
                return Ok changedRows
            with
            | :? DbUpdateConcurrencyException -> return Error Repository.ConcurrentDataAccess
            
            | :? DbUpdateException as ex when ex.InnerException.Message.Contains("ix_users_username") ->
                return Error <| Repository.DuplicateIndex (nameof user.Username)
            
            | :? DbUpdateException as ex when ex.InnerException.Message.Contains("ix_users_email") ->
                return Error <| Repository.DuplicateIndex (nameof user.Email)
            
            | :? DbUpdateException as ex when ex.InnerException.Message.Contains("ix_users_phone") ->
                return Error <| Repository.DuplicateIndex (nameof user.PhoneNumber)
        }
}
