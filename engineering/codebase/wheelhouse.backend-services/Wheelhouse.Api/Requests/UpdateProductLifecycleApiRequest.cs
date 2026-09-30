using Wheelhouse.Domain.Products.Enums;

namespace Wheelhouse.Api.Requests;

/// <summary>Represents the body of a request to record where a catalog product stands.</summary>
public sealed record UpdateProductLifecycleApiRequest
{
    /// <summary>Gets where the product now stands.</summary>
    public required ProductLifecycle Lifecycle { get; init; }
}
