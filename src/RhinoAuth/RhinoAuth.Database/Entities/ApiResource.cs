namespace RhinoAuth.Database;

public class ApiResource : BaseEntityString
{
	public required string DisplayName { get; set; }
	public string? Logo { get; set; }
	public bool IsActive { get; set; }
	public List<string> Scopes { get; set; } = [];

	// This is not being used, but the idea was:
	// For better performance, a shared secret can be agreed with the API resource
	// if set, symmetric signing will be used using this key
	public string? SymmetricJwtSecret { get; set; }

	public ICollection<ApiClientResource> ApiClientResources { get; set; } = null!;
	public ICollection<ExternalLoginApiResource> ExternalLoginApiResources { get; set; } = null!;
	public ICollection<AuthorizeRequestApiResource> AuthorizeRequestApiResources { get; set; } = null!;
	public ICollection<TokenRequestApiResource> TokenRequestApiResources { get; set; } = null!;
}