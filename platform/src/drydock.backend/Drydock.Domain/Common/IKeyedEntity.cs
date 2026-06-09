namespace Drydock.Domain.Common;

/// <summary>An entity identified by a strongly-typed key.</summary>
/// <typeparam name="TKey">The identifier type.</typeparam>
public interface IKeyedEntity<out TKey> : IEntity
{
    /// <summary>Gets the entity's unique identifier.</summary>
    TKey Id { get; }
}
