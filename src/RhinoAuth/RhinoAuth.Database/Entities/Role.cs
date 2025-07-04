using Microsoft.EntityFrameworkCore;

namespace RhinoAuth.Database;

public class Role : BaseEntityString
{
	public string? DisplayName { get; set; }
	public string? Description { get; set; }

	public ICollection<RoleClaim> RoleClaims { get; set; } = null!;
	public ICollection<UserRole> UserRoles { get; set; } = null!;
}

[PrimaryKey(nameof(RoleId), nameof(ClaimId))]
public class RoleClaim
{
	public required string RoleId { get; set; }
	public Role Role { get; set; } = null!;

	public required string ClaimId { get; set; }
	public AppClaim Claim { get; set; } = null!;
}