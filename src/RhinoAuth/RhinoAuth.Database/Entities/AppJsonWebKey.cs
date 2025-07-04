namespace RhinoAuth.Database;

public class AppJsonWebKey : BaseEntityString
{
	public bool IsActive { get; set; } = true;
	public JwkType Type { get; set; }
	public required string Curve { get; set; }
	public required string X { get; set; }
	public required string Y { get; set; }
	public required string D { get; set; }
}

public enum JwkType
{
	EC,
	RSA,
	oct
}