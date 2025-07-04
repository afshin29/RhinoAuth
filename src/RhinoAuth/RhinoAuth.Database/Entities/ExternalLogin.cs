using Microsoft.EntityFrameworkCore;
using System.Net;

namespace RhinoAuth.Database;

public class ExternalLogin : BaseEntityString
{
	public required IPAddress IpAddress { get; set; }
	public string? UserAgent { get; set; }
	public string? AccessToken { get; set; }
	public string? RefreshToken { get; set; }
	public string? IdToken { get; set; }
	public List<string> OpenIdScopes { get; set; } = [];
	public DateTimeOffset? UpdatedAt { get; set; }
	public string? PreviousRefreshToken { get; set; }

	public long UserId { get; set; }
	public User User { get; set; } = null!;

	public required string LoginId { get; set; }
	public Login Login { get; set; } = null!;

	public required string ApiClientId { get; set; }
	public ApiClient ApiClient { get; set; } = null!;

	public ICollection<ExternalLoginApiResource> ExternalLoginApiResources { get; set; } = null!;
	public ICollection<ApiClientHttpCall> ApiClientHttpCalls { get; set; } = null!;
}

[PrimaryKey(nameof(ExternalLoginId), nameof(ApiResourceId))]
public class ExternalLoginApiResource
{
	public required string ExternalLoginId { get; set; }
	public ExternalLogin External { get; set; } = null!;

	public required string ApiResourceId { get; set; }
	public ApiResource ApiResource { get; set; } = null!;
}