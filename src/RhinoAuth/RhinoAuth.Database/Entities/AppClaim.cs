namespace RhinoAuth.Database;

public class AppClaim : BaseEntityString
{
	public string? DisplayName { get; set; }
	public string? Description { get; set; }
	public required string Group { get; set; }

	public ICollection<RoleClaim> RoleClaims { get; set; } = null!;
}