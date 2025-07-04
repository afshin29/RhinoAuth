using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.ComponentModel.DataAnnotations.Schema;
using System.Net;

namespace RhinoAuth.Database;

[Index(nameof(Username), IsUnique = true)]
[Index(nameof(Email), IsUnique = true)]
[Index(nameof(PhoneNumber), nameof(CountryPhoneCode), IsUnique = true)]
public class User : BaseEntity<long>, IEntityTypeConfiguration<User>
{
	public required string Username { get; set; }
	public required string PasswordHash { get; set; }
	public int CountryPhoneCode { get; set; }
	public required string PhoneNumber { get; set; }
	public required string Email { get; set; }
	public required string FirstName { get; set; }
	public required string LastName { get; set; }
	public string? Avatar { get; set; }
	public List<ProfileUpdate> ProfileUpdateHistory { get; set; } = [];
	public DateTimeOffset? BlockedAt { get; set; }
	public DateTimeOffset? LockoutEndsAt { get; set; }
	public int FailedLoginAttempts { get; set; }
	public string? TotpSecret { get; set; }
	public Dictionary<string, string>? DomainAttributes { get; set; }
	public string? UnverifiedCountryCode { get; set; }
	public int? UnverifiedCountryPhoneCode { get; set; }
	public string? UnverifiedPhoneNumber { get; set; }
	public string? UnverifiedEmail { get; set; }

	public required string CountryCode { get; set; }
	[ForeignKey(nameof(CountryCode))]
	public Country Country { get; set; } = null!;

	public long? CreatorId { get; set; }
	public User? Creator { get; set; }

	public ICollection<OneTimeCode> OneTimeCodes { get; set; } = null!;
	public ICollection<Login> Logins { get; set; } = null!;
	public ICollection<ExternalLogin> ExternalLogins { get; set; } = null!;
	public ICollection<UserRole> UserRoles { get; set; } = null!;
	public ICollection<AuthorizeRequest> OauthRequests { get; set; } = null!;

	public record ProfileUpdate(
		DateTimeOffset CreatedAt,
		string FirstName,
		string LastName,
		string? Avatar);

	public void AddProfileHistoryRow()
		=> ProfileUpdateHistory.Add(new(
			DateTimeOffset.UtcNow,
			FirstName,
			LastName,
			Avatar));

	public void Configure(EntityTypeBuilder<User> builder)
	{
		builder.OwnsManyJson(x => x.ProfileUpdateHistory);
	}
}

public class OneTimeCode : BaseEntityString
{
	public required string Code { get; set; }
	public required string Reason { get; set; }
	public bool IsUsed { get; set; }
	public int FailedAttempts { get; set; }
	public required IPAddress IpAddress { get; set; }
	public string? UserAgent { get; set; }

	public long UserId { get; set; }
	public User User { get; set; } = null!;
}

[PrimaryKey(nameof(RoleId), nameof(UserId))]
public class UserRole
{
	public required string RoleId { get; set; }
	public Role Role { get; set; } = null!;

	public long UserId { get; set; }
	public User User { get; set; } = null!;
}

public static class OneTimeCodeReason
{
	public const string PhoneNumber = nameof(PhoneNumber);
	public const string Email = nameof(Email);
	public const string Password = nameof(Password);
	public const string DeleteAccount = nameof(DeleteAccount);
}