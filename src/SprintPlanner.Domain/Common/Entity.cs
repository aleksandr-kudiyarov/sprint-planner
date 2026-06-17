namespace SprintPlanner.Domain.Common;

/// <summary>
/// Base class for persistent aggregate entities keyed by a <see cref="Guid"/>.
/// </summary>
public abstract class Entity
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public override bool Equals(object? obj)
        => obj is Entity other && other.GetType() == GetType() && other.Id == Id && Id != Guid.Empty;

    public override int GetHashCode() => Id.GetHashCode();
}
