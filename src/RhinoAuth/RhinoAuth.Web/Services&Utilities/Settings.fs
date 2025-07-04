namespace Settings


[<CLIMutable>]
type AppLimits = {
    RegistrationAttemptLimit: int
    IpAttemptLimit: int
    PhoneNumberAttemptLimit: int
    AllowUnsafeNetworks: bool
    CodeRetryLimit: int
    CodeExpirationInMinute: int
    UnusedCodesLimit: int
    WrongPasswordLimit: int
    AccountLockoutInMinute: int
    ResetPasswordLimitPerDay: int
    ChangeProfileLimitPerDay: int
    ChangePhoneNumberLimitPerDay: int
    ChangeEmailLimitPerDay: int
    UnfinishedOAuthRequestLimitPerHour: int
}


[<CLIMutable>]
type AppBehavior = {
    PublishEvents: bool
    UseRevokedTokensCache: bool
    OAuthIssuer: string
}