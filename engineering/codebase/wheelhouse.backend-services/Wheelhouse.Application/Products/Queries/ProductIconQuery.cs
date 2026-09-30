using Wheelhouse.Application.Abstractions;
using WoW.Two.Sdk.Backend.Beta.Mediator.Cqrs;
using WoW.Two.Sdk.Backend.Beta.Mediator.Result;

namespace Wheelhouse.Application.Products.Queries;

/// <summary>Represents a query to get the icon a catalog product's repository carries.</summary>
public sealed record ProductIconQuery : IQuery<AppResult<ProductIconImage>>
{
    /// <summary>Gets the product's slug.</summary>
    public required string Slug { get; init; }
}
