using System.ComponentModel.DataAnnotations;

namespace RhinoAuth.Database;

public abstract class BaseEntity
{
	public virtual object Id { get; init; } = new();
	public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
	[Timestamp]
	public uint Version { get; set; }
}

public abstract class BaseEntity<TKey> : BaseEntity
	where TKey : struct, IComparable, IEquatable<TKey>
{
	public new TKey Id { get; init; }
}

public abstract class BaseEntityString : BaseEntity
{
	public new required string Id { get; init; }
}