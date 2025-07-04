using System.ComponentModel.DataAnnotations.Schema;
using System.Net;

namespace RhinoAuth.Database;

public class SignupRequest : BaseEntityString
{
	public required IPAddress IpAddress { get; set; }
	public string? UserAgent { get; set; }
	public int CountryPhoneCode { get; set; }
	public required string PhoneNumber { get; set; }
	public required string Email { get; set; }
	public required string Username { get; set; }
	public required string PasswordHash { get; set; }
	public required string FirstName { get; set; }
	public required string LastName { get; set; }
	public required string EmailVerificationCode { get; set; }
	public string? SmsVerificationCode { get; set; }
	public DateTimeOffset ExpiresAt { get; set; }
	public int FailedAttempts { get; set; }

	public required string CountryCode { get; set; }
	[ForeignKey(nameof(CountryCode))]
	public Country Country { get; set; } = null!;
}