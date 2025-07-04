module Routing

open Dependencies
open FsToolkit.ErrorHandling
open Giraffe
open Giraffe.EndpointRouting
open Giraffe.ViewEngine
open Microsoft.AspNetCore.Antiforgery
open Microsoft.AspNetCore.Authentication
open Microsoft.AspNetCore.Authentication.Cookies
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.Configuration
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.Logging
open Microsoft.Extensions.Options
open RhinoAuth.Database
open Settings
open System.Security.Claims
open View
open Workflows

open FormModels
open WorkflowBuilder


let private getViewData (ctx: HttpContext) : ViewData =
    {
        CurrentPath = ctx.Request.Path + ctx.Request.QueryString
        User = ctx.User
        IsDarkMode = ctx.Request.IsDarkMode()
        Language = "en"
        IsRTL = false
        AntiforgeryTokenProvider = ctx.GetAntiforgeryToken
        QueryParameterProvider = ctx.TryGetQueryStringValue
        IsDevEnv = ctx.GetWebHostEnvironment().IsDevelopment()
    }


let private getWorkflowData (ctx: HttpContext) : WorkflowBuilder.WorkflowData =

    let dbContext = ctx.RequestServices.GetRequiredService<RhinoDbContext>()

    {
        DependencyProvider = {
            IpInfoProvider = IpInfo.getIpInfoProvider ctx
            CaptchaValidator = Capthca.getCaptchaValidator ctx
            EmailSender = EmailSender.getEmailSender ctx
            SmsSender = SmsSender.getSmsSender ctx
            FileManager = FileManager.getFileManager ctx
            DistributedCache = DistributedCache.getDistributedCache ctx
        }
        AppLimits = ctx.GetService<IOptions<AppLimits>>().Value
        AppBehavior = ctx.GetService<IOptions<AppBehavior>>().Value
        DbCommands = DbAccess.EFCore.getCommands dbContext
        DbQueries = DbAccess.EFCore.getQueries dbContext
        HeaderProvider = ctx.TryGetRequestHeader
        User = ctx.User
        IpAddress = ctx.GetCurrentIpAddress()
        IsDevEnv = ctx.GetWebHostEnvironment().IsDevelopment()
    }


let private validateAntiforgeryToken : HttpHandler =
    fun next ctx -> task {
        let afService = ctx.GetService<IAntiforgery>()
        let! isValid = afService.IsRequestValidAsync ctx
        return!
            match isValid with
            | true -> next ctx
            | _ -> (setStatusCode 400 >=> text "Invalid anti-forgery token") next ctx
    }


let private requireLoggedInUser = requiresAuthentication <| challenge CookieAuthenticationDefaults.AuthenticationScheme


let private requireUnauthorizedUser : HttpHandler = fun next ctx -> if ctx.User.IsLoggedIn() then redirectTo false "/" next ctx else next ctx


let private requireQueryParam failHandler key : HttpHandler =
    fun next ctx ->
        match ctx.TryGetQueryStringValue key with
        | Some _ -> next ctx
        | None -> failHandler next ctx


let private requireQueryParamOrGoHome = requireQueryParam <| redirectTo false "/"


let private page (viewFunc: ViewData -> XmlNode) : HttpHandler =
    fun next ctx ->
        let view = getViewData ctx |> viewFunc
        htmlView view next ctx


let private pageWithForm formModel errorMsg (viewFunc: 'a option -> string option -> ViewData -> XmlNode) : HttpHandler =
    fun next ctx ->
        let view = getViewData ctx |> viewFunc formModel errorMsg
        htmlView view next ctx


let private pageWithFormEmpty viewFunc = pageWithForm None None viewFunc


let private swithThemeHandler : HttpHandler =
    fun next ctx ->
        let newValue = if ctx.Request.IsDarkMode() then "0" else "1"
        let currentPath = ctx.Request.Form["current_path"].ToString()
        
        ctx.Response.Cookies.Append(DarkThemeKey, newValue, new Microsoft.AspNetCore.Http.CookieOptions(Expires = System.DateTimeOffset.Now.AddYears(1)))
        
        redirectTo false currentPath next ctx


let private workflowHandler formModel view workflow onSuccess : HttpHandler =
    fun next ctx -> task {
        let wd = getWorkflowData ctx
        let! result = workflow wd formModel

        return!
            match result with
            | Ok r -> (onSuccess r) next ctx

            | Error e ->
                let (statusCode, errorMsg) =
                    match e with
                    | FormValidationError msg -> (400, msg)
                    | ResourceNotFound -> (404, "Specified resource was not found")
                    | BusinessRuleError msg -> (409, msg)
                    | ConcurrencyError -> (400, "Concurrent requests are not supported")
                    | ExternalServiceError msg ->
                        let logger = ctx.GetLogger()
                        logger.LogCritical(msg)
                        (503, "Service is unavailable, please try again later")

                ctx.SetStatusCode statusCode
                pageWithForm (Some formModel) (Some errorMsg) view next ctx
    }



let private workflowHandlerOf<'a, 'b> view (workflow: _ -> 'a -> TaskResult<'b, WorkflowError>) onSuccess : HttpHandler =
    fun next ctx -> task {
        let! bindResult = ctx.TryBindFormAsync<'a>()

        match bindResult with
        | Error e ->
            let! invalidFormModel = ctx.BindFormAsync<'a>()
            ctx.SetStatusCode 400
            return! pageWithForm (Some invalidFormModel) (Some e) view next ctx

        | Ok formModel ->
            return! (workflowHandler formModel view workflow onSuccess) next ctx
    }


let private getPageHandlerWithDependency (ctx: HttpContext) dependency pageHandler view =
    dependency
    |> Task.map(function
        | Ok result -> pageHandler (view result)
        | Error msg ->
            let logger = ctx.GetLogger()
            logger.LogCritical(msg)
            page Pages.StaticErrors.serviceUnavailable
    )



let endpoints = [

    GET_HEAD [
        route "/" (page Pages.Home.func)

        route "/oidc-logout" (page Pages.Logout.func)
        
        route "/signup" (
            requireUnauthorizedUser
            >=> (fun next ctx -> task {
                let countries = ctx.RequestServices.GetRequiredService<Services.CountryCache>().Countries
                
                let ipCountryCode =
                    ctx.GetCurrentIpAddress()
                    |> IpInfo.getIpInfoProvider ctx
                    |> TaskResult.map _.CountryCode
                
                let view = Pages.Signup.func countries

                let! handler = getPageHandlerWithDependency ctx ipCountryCode pageWithFormEmpty view
                return! handler next ctx
            })
        )

        route "/signup/verify-email" (
            requireQueryParamOrGoHome "signupId"
            >=> pageWithFormEmpty Pages.SignupVerifications.verifyEmail
        )
        
        route "/signup/verify-phone" (
            requireQueryParamOrGoHome "signupId"
            >=> pageWithFormEmpty Pages.SignupVerifications.verifyPhone
        )
        
        route "/login" (
            requireUnauthorizedUser
            >=> pageWithFormEmpty Pages.Login.func
        )
        
        route "/forgot-password" (
            requireUnauthorizedUser
            >=> pageWithFormEmpty Pages.PasswordRecovery.forgotPassword)
        
        route "/reset-password" (
            requireUnauthorizedUser
            >=> requireQueryParamOrGoHome "codeId"
            >=> pageWithFormEmpty Pages.PasswordRecovery.resetPassword
        )
        
        route "/profile" (
            requireLoggedInUser
            >=> (fun next ctx -> task {
                let userId = ctx.User.GetRequiredId()
                let dbContext = ctx.RequestServices.GetRequiredService<RhinoDbContext>()
                let! profileVM = DbAccess.EFCore.getQueries(dbContext).GetProfileVM userId

                return! pageWithFormEmpty (Pages.Profile.func profileVM) next ctx
            })
        )

        route "/profile/change-name" (
            requireLoggedInUser
            >=> (fun next ctx -> task {
                let userId = ctx.User.GetRequiredId()
                let dbContext = ctx.RequestServices.GetRequiredService<RhinoDbContext>()
                let! user = DbAccess.EFCore.getQueries(dbContext).GetUser userId

                let formModel: ChangeNameFM = {
                    FirstName = user.Value.FirstName
                    LastName = user.Value.LastName
                }

                return! pageWithForm (Some formModel) None Pages.ProfileForms.changeName next ctx
            })
        )

        route "/profile/change-phone" (
            requireLoggedInUser
            >=> (fun next ctx -> task {
                let userId = ctx.User.GetRequiredId()
                let dbContext = ctx.RequestServices.GetRequiredService<RhinoDbContext>()
                let! user = DbAccess.EFCore.getQueries(dbContext).GetUser userId

                let formModel: RequestPhoneNumberChangeFM = {
                    PhoneCode = user.Value.CountryPhoneCode
                    PhoneNumber = user.Value.PhoneNumber
                }

                let countries = ctx.RequestServices.GetRequiredService<Services.CountryCache>().Countries
                
                return! pageWithForm (Some formModel) None (Pages.ProfileForms.changePhoneNumber countries) next ctx
            })
        )

        route "/profile/change-email" (
            requireLoggedInUser
            >=> (fun next ctx -> task {
                let userId = ctx.User.GetRequiredId()
                let dbContext = ctx.RequestServices.GetRequiredService<RhinoDbContext>()
                let! user = DbAccess.EFCore.getQueries(dbContext).GetUser userId

                let formModel: RequestEmailChangeFM = {
                    Email = user.Value.Email
                }

                return! pageWithForm (Some formModel) None Pages.ProfileForms.changeEmail next ctx
            })
        )

        route "/profile/verify-phone-code" (
            requireLoggedInUser
            >=> requireQueryParam (redirectTo false "/profile") "codeId"
            >=> pageWithFormEmpty (Pages.ProfileForms.verifyCode "phone")
        )

        route "/profile/verify-email-code" (
            requireLoggedInUser
            >=> requireQueryParam (redirectTo false "/profile") "codeId"
            >=> pageWithFormEmpty (Pages.ProfileForms.verifyCode "email")
        )

        route "/profile/change-password" (
            requireLoggedInUser
            >=> pageWithFormEmpty Pages.ProfileForms.changePassword
        )
    ]

    POST [
        route "/switch-theme" (
            validateAntiforgeryToken
            >=> swithThemeHandler
        )

        route "/oidc-logout" (
            validateAntiforgeryToken
            >=> (fun next ctx -> task {
                let! formModel = ctx.BindFormAsync<OAuthModels.LogoutFM>()

                let wd = getWorkflowData ctx
                let! redirectUri = LogoutWorkflow.entry wd formModel

                do! ctx.SignOutAsync()

                return! redirectTo false redirectUri next ctx
            })
        )
        
        route "/signup" (
            requireUnauthorizedUser
            >=> validateAntiforgeryToken
            >=> (fun next ctx -> task {
                let onSuccess id = 
                    let returnUrlParam =
                        ctx.TryGetQueryStringValue "returnUrl"
                        |> Option.map (fun x -> $"&returnUrl={x}")
                        |> Option.defaultValue ""
                    
                    redirectTo false $"/signup/verify-email?signupId={id}{returnUrlParam}"

                let countries = ctx.RequestServices.GetRequiredService<Services.CountryCache>().Countries
                
                // for selected country code, form model will be used
                let view = Pages.Signup.func countries "" 
                
                let workflowWithIpCountry wd formModel =
                    ctx.GetCurrentIpAddress()
                    |> IpInfo.getIpInfoProvider ctx
                    |> mapDependencyError
                    |> TaskResult.bind (fun ipCountry -> SignupWorkflow.entry ipCountry wd formModel)

                return! (workflowHandlerOf<SignupFM, string> view workflowWithIpCountry onSuccess) next ctx
            })
        )
        
        route "/signup/verify-email" (
            requireUnauthorizedUser
            >=> validateAntiforgeryToken
            >=> (fun next ctx -> task {
                let onSuccess id =
                    let returnUrlParam =
                        ctx.TryGetQueryStringValue "returnUrl"
                        |> Option.map (fun x -> $"&returnUrl={x}")
                        |> Option.defaultValue ""
                    
                    redirectTo false $"/signup/verify-phone?signupId={id}{returnUrlParam}"

                return! (workflowHandlerOf<SignupVerificationFM, string> Pages.SignupVerifications.verifyEmail SignupVerificationWorkflows.emailVerificationWorkflow onSuccess) next ctx
            })
        )
        
        route "/signup/verify-phone" (
            requireUnauthorizedUser
            >=> validateAntiforgeryToken
            >=> (fun next ctx -> task {
                let onSuccess _ =
                    match ctx.TryGetQueryStringValue "returnUrl" with
                    | Some path when path.StartsWith('/') -> path
                    | _ -> "/login"
                    
                    |> redirectTo false

                return! (workflowHandlerOf<SignupVerificationFM, string> Pages.SignupVerifications.verifyPhone SignupVerificationWorkflows.phoneVerificationWorkflow onSuccess) next ctx
            })
        )
                
        route "/login" (
            requireUnauthorizedUser
            >=> validateAntiforgeryToken
            >=> (fun next ctx ->
                let onSuccess (claims, isPersistent) =
                    let claimsIdentity = ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme)
                    let claimsPrincipal = ClaimsPrincipal([claimsIdentity])
                    
                    ctx.SignInAsync(claimsPrincipal, AuthenticationProperties(IsPersistent = isPersistent)).Wait()
                    
                    match ctx.TryGetQueryStringValue "returnUrl" with
                    | Some path when path.StartsWith('/') -> path
                    | _ -> "/"
                    
                    |> redirectTo false

                let workflowWithIpCountry wd formModel =
                    ctx.GetCurrentIpAddress()
                    |> Dependencies.IpInfo.getIpInfoProvider ctx
                    |> mapDependencyError
                    |> TaskResult.bind (fun ipCountry -> LoginWorkflow.entry ipCountry wd formModel)

                workflowHandlerOf<LoginFM, (Claim list * bool)> Pages.Login.func workflowWithIpCountry onSuccess next ctx
            )
        )
        
        route "/forgot-password" (
            requireUnauthorizedUser
            >=> validateAntiforgeryToken
            >=> (fun next ctx ->
                let onSuccess id =
                    let returnUrlParam =
                        ctx.TryGetQueryStringValue "returnUrl"
                        |> Option.map (fun x -> $"&returnUrl={x}")
                        |> Option.defaultValue ""
                    
                    redirectTo false $"/reset-password?codeId={id}{returnUrlParam}"

                workflowHandlerOf<ForgotPasswordFM, string> Pages.PasswordRecovery.forgotPassword PasswordRecoveryWorkflows.forgotPasswordWorkflow onSuccess next ctx
            )
        )
        
        route "/reset-password" (
            requireUnauthorizedUser
            >=> validateAntiforgeryToken
            >=> (fun next ctx ->
                let onSuccess _ =
                    let returnUrlParam =
                        ctx.TryGetQueryStringValue "returnUrl"
                        |> Option.map (fun x -> $"?returnUrl={x}")
                        |> Option.defaultValue ""
                    
                    redirectTo false $"/login{returnUrlParam}"

                workflowHandlerOf<ResetPasswordFM, _> Pages.PasswordRecovery.resetPassword PasswordRecoveryWorkflows.resetPasswordWorkflow onSuccess next ctx
            )
        )
        
        route "/change-avatar" (
            requireLoggedInUser
            >=> validateAntiforgeryToken
            >=> (fun next ctx -> task {
                let onSuccess _ = redirectTo false "/profile"

                let userId = ctx.User.GetRequiredId()
                let dbContext = ctx.RequestServices.GetRequiredService<RhinoDbContext>()
                let! profileVM = DbAccess.EFCore.getQueries(dbContext).GetProfileVM userId

                if not ctx.Request.HasFormContentType then
                    ctx.SetStatusCode 400
                    return! pageWithFormEmpty (Pages.Profile.func profileVM) next ctx
                
                else
                    let formModel: FormModels.ChangeAvatarFM = {
                        Avatar = ctx.Request.Form.Files.Item 0
                    }
                    
                    return! workflowHandler formModel (Pages.Profile.func profileVM) ProfileWorkflows.changeAvatarWorkflow onSuccess next ctx
            })
        )
        
        route "/remove-avatar" (
            requireLoggedInUser
            >=> validateAntiforgeryToken
            >=> (fun next ctx -> task {
                let onSuccess _ = redirectTo false "/profile"

                let userId = ctx.User.GetRequiredId()
                let dbContext = ctx.RequestServices.GetRequiredService<RhinoDbContext>()
                let! profileVM = DbAccess.EFCore.getQueries(dbContext).GetProfileVM userId

                return! workflowHandler () (Pages.Profile.func profileVM) ProfileWorkflows.removeAvatarWorkflow onSuccess next ctx
            })
        )
        
        route "profile/change-name" (
            requireLoggedInUser
            >=> validateAntiforgeryToken
            >=> (fun next ctx ->
                let onSuccess _ = redirectTo false "/profile"

                workflowHandlerOf<ChangeNameFM, _> Pages.ProfileForms.changeName ProfileWorkflows.changeNameWorkflow onSuccess next ctx
            )
        )
        
        route "profile/change-phone" (
            requireLoggedInUser
            >=> validateAntiforgeryToken
            >=> (fun next ctx ->
                let onSuccess codeId = redirectTo false $"/profile/verify-phone-code?codeId={codeId}"
                
                let countries = ctx.RequestServices.GetRequiredService<Services.CountryCache>().Countries

                workflowHandlerOf<RequestPhoneNumberChangeFM, _> (Pages.ProfileForms.changePhoneNumber countries) ProfileWorkflows.requestPhoneNumberChangeWorkflow onSuccess next ctx
            )
        )
        
        route "profile/change-email" (
            requireLoggedInUser
            >=> validateAntiforgeryToken
            >=> (fun next ctx ->
                let onSuccess codeId = redirectTo false $"/profile/verify-email-code?codeId={codeId}"

                workflowHandlerOf<RequestEmailChangeFM, _> Pages.ProfileForms.changeEmail ProfileWorkflows.requestEmailChangeWorkflow onSuccess next ctx
            )
        )
        
        route "profile/verify-phone-code" (
            requireLoggedInUser
            >=> validateAntiforgeryToken
            >=> (fun next ctx ->
                let onSuccess _ = redirectTo false $"/profile"

                workflowHandlerOf<VerifyCodeFM, _> (Pages.ProfileForms.verifyCode "phone") ProfileWorkflows.verifyPhoneNumberCode onSuccess next ctx
            )
        )
        
        route "profile/verify-email-code" (
            requireLoggedInUser
            >=> validateAntiforgeryToken
            >=> (fun next ctx ->
                let onSuccess _ = redirectTo false $"/profile"

                workflowHandlerOf<VerifyCodeFM, _> (Pages.ProfileForms.verifyCode "email") ProfileWorkflows.verifyEmailCode onSuccess next ctx
            )
        )
        
        route "profile/change-password" (
            requireLoggedInUser
            >=> validateAntiforgeryToken
            >=> (fun next ctx ->
                let onSuccess _ = redirectTo false $"/profile"

                workflowHandlerOf<ChangePasswordFM, _> Pages.ProfileForms.changePassword ProfileWorkflows.changePasswordWorkflow onSuccess next ctx
            )
        )
    ]

    route "{*url}" (setStatusCode 404 >=> page Pages.StaticErrors.notFound)
]



let oauthEndpoints = [

    GET [
        route "/oauth/authorize" (
            requireLoggedInUser
            >=> (fun next ctx -> task {

                match ctx.TryBindQueryString<OAuthModels.AuthorizeRequest>() with
                | Error msg ->
                    ctx.SetStatusCode 422
                    return! page Pages.Authorize.unprocessableRequest next ctx
                
                | Ok model ->

                    let wd = getWorkflowData ctx

                    let! dbData = wd.DbQueries.GetAuthorizeData model.client_id (ctx.User.GetRequiredId())

                    match dbData.ApiClient with
                    | None ->
                        ctx.SetStatusCode 400
                        return! page Pages.Authorize.invalidClient next ctx

                    | Some apiClient ->

                        let! authorizeResult = AuthorizeWorkflow.entry wd model apiClient dbData.UnfinishedRequestsCount
                        
                        match authorizeResult with
                        | Error (code, msg) -> return! redirectTo false $"{apiClient.LoginCallbackUri}?error={code.ToString()}&error_description={msg}" next ctx
                        
                        | Ok result -> 
                            match result with
                            | AuthorizeWorkflow.ShowConsent code -> return! redirectTo false $"/oauth/authorize/consent?code={code}" next ctx
                            
                            | AuthorizeWorkflow.Redirect code -> return! redirectTo false $"{apiClient.LoginCallbackUri}?code={code}&iss={wd.AppBehavior.OAuthIssuer}" next ctx
                            
                            | AuthorizeWorkflow.RedirectWithState (code, state) -> return! redirectTo false $"{apiClient.LoginCallbackUri}?code={code}&state={state}&iss={wd.AppBehavior.OAuthIssuer}" next ctx
            })
        )

        route "/oauth/authorize/consent" (
            requireLoggedInUser
            >=> requireQueryParamOrGoHome "code"
            >=> (fun next ctx -> task {
                let userId = ctx.User.GetRequiredId()
                let code = ctx.TryGetQueryStringValue "code" |> Option.defaultValue ""

                let dbContext = ctx.RequestServices.GetRequiredService<RhinoDbContext>()
                let! consentVM = DbAccess.EFCore.getQueries(dbContext).GetAuthorizeConsentVM code userId

                return!
                    match consentVM with
                    | None -> redirectTo false "/" next ctx
                    | Some vm -> pageWithFormEmpty (Pages.Authorize.consent vm) next ctx
            })
        )

        route "/.well-known/openid-configuration" (
            fun next ctx ->
                let wd = getWorkflowData ctx
                let issuer = wd.AppBehavior.OAuthIssuer
                let response = {|
                    issuer = issuer
                    authorization_endpoint = $"{issuer}/oauth/authorize"
                    end_session_endpoint = $"{issuer}/oidc-logout"
                    token_endpoint = $"{issuer}/oauth/token"
                    jwks_uri = $"{issuer}/public-keys"
                    backchannel_logout_supported = true
                    backchannel_logout_session_supported = true
                    response_types_supported = [ "code" ]
                    subject_types_supported = [ "public" ]
                    id_token_signing_alg_values_supported = [ "ES256" ]
                    code_challenge_methods_supported = [ "plain", "S256" ]
                    grant_types_supported = [
                        "authorization_code"
                        "client_credentials"
                        "refresh_token"
                    ]
                |}

                json response next ctx
        )

        route "/public-keys" (
            fun next ctx -> task {
                let wd = getWorkflowData ctx
                let! keys = wd.DbQueries.GetPublicKeys()

                return! json keys next ctx
            }
        )
    ]

    POST [

        let consentHandler workflow : HttpHandler =
            fun next ctx -> task {

                let userId = ctx.User.GetRequiredId()
                let code = ctx.TryGetQueryStringValue "code" |> Option.defaultValue ""

                let dbContext = ctx.RequestServices.GetRequiredService<RhinoDbContext>()
                let! consentVM = DbAccess.EFCore.getQueries(dbContext).GetAuthorizeConsentVM code userId

                let onSuccess url = redirectTo false url

                return!
                    match consentVM with
                    | None -> redirectTo false "/" next ctx
                    | Some vm -> workflowHandlerOf<OAuthModels.ConsentFM, string> (Pages.Authorize.consent vm) workflow onSuccess next ctx
            }

        route "/oauth/authorize/accept" (
            requireLoggedInUser
            >=> validateAntiforgeryToken
            >=> consentHandler ConsentWorkflows.acceptAuthorizeRequest
        )

        route "/oauth/authorize/reject" (
            requireLoggedInUser
            >=> validateAntiforgeryToken
            >=> consentHandler ConsentWorkflows.rejectAuthorizeRequest
        )

        route "/oauth/token" (
            fun next ctx -> task {
                let! bindResult = ctx.TryBindFormAsync<OAuthModels.TokenRequest>()

                match bindResult with
                | Error msg ->
                    let error = {|
                        error = OAuthModels.ProtocolError.invalid_request.ToString()
                        error_description = msg
                    |}

                    return! json error next ctx

                | Ok formModel ->
                    let wd = getWorkflowData ctx
                    let! result = TokenWorkflow.entry wd formModel

                    match result with
                    | Error (code, msg) ->
                        let error = {|
                            error = code.ToString()
                            error_description = msg
                        |}
                        
                        return! json error next ctx

                    | Ok successResponse ->
                        return! json successResponse next ctx
            }
        )
    ]
]