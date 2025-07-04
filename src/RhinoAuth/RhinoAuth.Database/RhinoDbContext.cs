using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.IdentityModel.Tokens;
using System.Linq.Expressions;
using System.Security.Cryptography;

namespace RhinoAuth.Database;

// We are using C# and EF Core to define our database and handle our migrations
// but it doesn't mean that we have to use them for our commands and queries
// clients of this database are free to choose their language and data access library

public class RhinoDbContext(DbContextOptions<RhinoDbContext> options) : DbContext(options), IDataProtectionKeyContext
{
	// Data protection key table
	public DbSet<DataProtectionKey> DataProtectionKeys { get; set; }

	public DbSet<ApiClient> ApiClients { get; set; }
	public DbSet<ApiClientResource> ApiClientResources { get; set; }
	public DbSet<ApiClientTokenRequest> ApiClientTokenRequests { get; set; }
	public DbSet<ApiResource> ApiResources { get; set; }
	public DbSet<AppClaim> AppClaims { get; set; }
	public DbSet<AppJsonWebKey> AppJsonWebKeys { get; set; }
	public DbSet<AuthorizeRequest> AuthorizeRequests { get; set; }
	public DbSet<Country> Countries { get; set; }
	public DbSet<ExternalLogin> ExternalLogins { get; set; }
	public DbSet<Login> Logins { get; set; }
	public DbSet<OneTimeCode> OneTimeCodes { get; set; }
	public DbSet<Role> Roles { get; set; }
	public DbSet<RoleClaim> RoleClaims { get; set; }
	public DbSet<SignupRequest> SignupRequests { get; set; }
	public DbSet<User> Users { get; set; }
	public DbSet<UserRole> UserRoles { get; set; }

	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		base.OnModelCreating(modelBuilder);

		modelBuilder.ApplyConfigurationsFromAssembly(this.GetType().Assembly);
	}

	public static Action<DbContext, bool> SeederFunc = (dbContext, _) =>
	{
		var countryTable = dbContext.Set<Country>();

		if (!countryTable.Any())
		{
			var countriesJson = File.ReadAllText("../RhinoAuth.Database/countries.json");
			var countries = System.Text.Json.JsonSerializer
				.Deserialize<IEnumerable<SeedCountry>>(countriesJson)!
				.Select(c => new Country
				{
					Code = c.TwoLetterCode,
					Name = c.Name,
					PhoneCode = int.Parse(c.PhoneCode),
					AllowIpLogin = true,
					AllowIpResgistration = true,
					AllowPhoneNumberLogin = true,
					AllowPhoneNumberResgistration = true
				})
				.OrderBy(c => c.Name);

			countryTable.AddRange(countries);
			dbContext.SaveChanges();
		}

		var jwkTable = dbContext.Set<AppJsonWebKey>();

		if (!jwkTable.Any())
		{
			using ECDsa ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
				
			// Generate the key pair
			ecdsa.GenerateKey(ECCurve.NamedCurves.nistP256);

			// Export the private and public key as ECDsaParameters
			var privateKey = ecdsa.ExportParameters(true); // true for private key
			var publicKey = ecdsa.ExportParameters(false); // false for public key only

			jwkTable.Add(new AppJsonWebKey
			{
				Id = Guid.NewGuid().ToString(),
				Type = JwkType.EC,
				Curve = "P-256",
				X = Base64UrlEncoder.Encode(publicKey.Q.X),
				Y = Base64UrlEncoder.Encode(publicKey.Q.Y),
				D = Base64UrlEncoder.Encode(privateKey.D)
			});
			dbContext.SaveChanges();
		}
	};

	record SeedCountry(string Name, string TwoLetterCode, string PhoneCode);
}

internal static class EFExtensions
{
	public static EntityTypeBuilder<TEntity> OwnsManyJson<TEntity, TJson>(
		this EntityTypeBuilder<TEntity> builder,
		Expression<Func<TEntity, IEnumerable<TJson>?>> selector)
		where TEntity : class
		where TJson : class
		=> builder.OwnsMany(selector, nav => nav.ToJson());

	public static EntityTypeBuilder<TEntity> OwnsOneJson<TEntity, TJson>(
		this EntityTypeBuilder<TEntity> builder,
		Expression<Func<TEntity, TJson?>> selector)
		where TEntity : class
		where TJson : class
		=> builder.OwnsOne(selector, nav => nav.ToJson());
}