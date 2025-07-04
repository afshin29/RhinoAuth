using Microsoft.EntityFrameworkCore;

namespace RhinoAuth.Database;

[PrimaryKey(nameof(Code))]
public class Country
{
	public required string Code { get; set; }
	public required string Name { get; set; }
	public int PhoneCode { get; set; }
	public bool AllowPhoneNumberResgistration { get; set; } = true;
	public bool AllowIpResgistration { get; set; } = true;
	public bool AllowPhoneNumberLogin { get; set; } = true;
	public bool AllowIpLogin { get; set; } = true;

	public ICollection<User> Users { get; set; } = null!;
	public ICollection<SignupRequest> SignupRequests { get; set; } = null!;
}