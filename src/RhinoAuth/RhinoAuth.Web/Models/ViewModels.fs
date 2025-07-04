module ViewModels


type ActiveSessionVM = {
    Device: string
    IpAddress: string
}

type ProfileVM = {
    Username: string
    Email: string
    CountryCode: string
    CountryPhoneCode: int
    PhoneNumber: string
    FirstName: string
    LastName: string
    Avatar: string option
    ActiveSessions: ActiveSessionVM array
}