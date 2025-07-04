using System.Net;

namespace RhinoAuth.Database;

public class Login : BaseEntityString
{
	public required IPAddress IpAddress { get; set; }
	public bool IsPersistent { get; set; }
	public string? UserAgent { get; set; }
	public DateTimeOffset? UpdatedAt { get; set; }
	public bool Successful { get; set; }
	public DateTimeOffset? EndedAt { get; set; }
	public string? EndedByExternalLoginId { get; set; }
	public IPAddress? LogoutIpAddress { get; set; }
	public long? TotpWindow { get; set; }

	public long UserId { get; set; }
	public User User { get; set; } = null!;

	public ICollection<ExternalLogin> ExternalLogins { get; set; } = null!;
	public ICollection<AuthorizeRequest> OauthRequests { get; set; } = null!;

	public bool IsValid() => !(EndedAt.HasValue || (UpdatedAt ?? CreatedAt).AddDays(30) < DateTimeOffset.UtcNow);
}