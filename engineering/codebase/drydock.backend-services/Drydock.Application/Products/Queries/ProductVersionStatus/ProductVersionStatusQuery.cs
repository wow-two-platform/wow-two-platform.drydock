using WoW.Two.Sdk.Backend.Beta.Mediator.Cqrs;
using WoW.Two.Sdk.Backend.Beta.Mediator.Result;

namespace Drydock.Application.Products.Queries.ProductVersionStatus;

/// <summary>Represents a query to resolve a product's ready build/image status from its repository.</summary>
/// <param name="ProductId">Product id.</param>
public sealed record ProductVersionStatusQuery(Guid ProductId)
    : IQuery<AppResult<ProductVersionStatusResult.Success, ProductVersionStatusResult.Failure>>;
