namespace Drydock.Application.Abstractions;

/// <summary>Provides the current UTC time — abstracts <see cref="DateTimeOffset.UtcNow"/> for testability.</summary>
public interface IClock
{
    /// <summary>Gets the current UTC instant.</summary>
    DateTimeOffset UtcNow { get; }
}
