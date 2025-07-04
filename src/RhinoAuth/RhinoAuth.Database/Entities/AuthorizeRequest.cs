using Microsoft.EntityFrameworkCore;

namespace RhinoAuth.Database;

public class AuthorizeRequest : BaseEntityString
{
	public AuthorizeRequestType RequestType { get; set; }
	public required string CodeChallenge { get; set; }
	public VerifierMethod VerifierMethod { get; set; }
	public required List<string> Scopes { get; set; }
	public string? State { get; set; }
	public string? Nonce { get; set; }
	public DateTimeOffset? ConsentedAt { get; set; }
	public DateTimeOffset? CompletedAt { get; set; }

	public required string LoginId { get; set; }
	public Login Login { get; set; } = null!;

	public long UserId { get; set; }
	public User User { get; set; } = null!;

	public required string ApiClientId { get; set; }
	public ApiClient ApiClient { get; set; } = null!;

	public ICollection<AuthorizeRequestApiResource> AuthorizeRequestApiResources { get; set; } = null!;
}

[PrimaryKey(nameof(AuthorizeRequestId), nameof(ApiResourceId))]
public class AuthorizeRequestApiResource
{
	public required string AuthorizeRequestId { get; set; }
	public AuthorizeRequest Authorize { get; set; } = null!;

	public required string ApiResourceId { get; set; }
	public ApiResource ApiResource { get; set; } = null!;
}

public enum VerifierMethod
{
	Plain,
	S256
}

public enum AuthorizeRequestType
{
	OpenId_OAuth,
	OpenId,
	OAuth
}