using Microsoft.EntityFrameworkCore;

namespace RhinoAuth.Database;

[PrimaryKey(nameof(ApiClientId), nameof(ApiResourceId))]
public class ApiClientResource
{
	// null means all of the resource's scopes, empty list means none of them
	public List<string>? AllowedScopes { get; set; }

	public required string ApiClientId { get; set; }
	public ApiClient ApiClient { get; set; } = null!;

	public required string ApiResourceId { get; set; }
	public ApiResource ApiResource { get; set; } = null!;
}
