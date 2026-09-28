using WoW.Two.Sdk.Backend.Beta.Mediator.Cqrs;
using WoW.Two.Sdk.Backend.Beta.Mediator.Result;
using Wheelhouse.Application.Abstractions;

namespace Wheelhouse.Application.Products.Queries.ProductIcon;

/// <summary>Represents a query for the icon a product's repository carries.</summary>
/// <param name="Id">Product id.</param>
public sealed record ProductIconQuery(Guid Id) : IQuery<AppResult<ProductIconImage>>;
