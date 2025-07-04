using Microsoft.EntityFrameworkCore;
using System.Net;

namespace RhinoAuth.Database;

public class ApiClient : BaseEntityString
{
	public required string DisplayName { get; set; }
	public string? Logo { get; set; }
	public bool IsActive { get; set; }
	public ApiClientType Type { get; set; }
	public string? Secret { get; set; }
	public required string Domain { get; set; }
	public required string LoginCallbackUri { get; set; }
	public required string LogoutCallbackUri { get; set; }
	public string? BackchannelLogoutUri { get; set; }
	public bool ShowConsent { get; set; }
	public DateTimeOffset? VerifiedAt { get; set; }
	public bool SupportsEcdsa { get; set; }

	public ICollection<ApiClientResource> ApiClientResources { get; set; } = null!;
	public ICollection<ExternalLogin> ExternalLogins { get; set; } = null!;
	public ICollection<ApiClientTokenRequest> TokenRequests { get; set; } = null!;
	public ICollection<ApiClientHttpCall> ApiClientHttpCalls { get; set; } = null!;
	public ICollection<AuthorizeRequest> AuthorizeRequests { get; set; } = null!;
}

public class ApiClientTokenRequest : BaseEntityString
{
	public required IPAddress IpAddress { get; set; }
	public required string AccessToken { get; set; }
	public required string RefreshToken { get; set; }
	public bool IsRefreshTokenUsed { get; set; }
	public List<string> Scopes { get; set; } = [];
	public string? RefreshedBy { get; set; }

	public required string ApiClientId { get; set; }
	public ApiClient ApiClient { get; set; } = null!;

	public ICollection<TokenRequestApiResource> TokenRequestApiResources { get; set; } = null!;
}

[PrimaryKey(nameof(TokenRequestId), nameof(ApiResourceId))]
public class TokenRequestApiResource
{
	public required string TokenRequestId { get; set; }
	public ApiClientTokenRequest TokenRequest { get; set; } = null!;

	public required string ApiResourceId { get; set; }
	public ApiResource ApiResource { get; set; } = null!;
}

public class ApiClientHttpCall : BaseEntityString
{
	public required string Address { get; set; }
	public string? Payload { get; set; }
	public int ResponseCode { get; set; }
	public string? ResponseBody { get; set; }

	public required string ExternalLoginId { get; set; }
	public ExternalLogin External { get; set; } = null!;

	public required string ApiClientId { get; set; }
	public ApiClient ApiClient { get; set; } = null!;
}

public enum ApiClientType
{
	Confidential,
	Public
}