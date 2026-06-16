using WoW.Two.Sdk.Backend.Beta.Mediator.Cqrs;
using WoW.Two.Sdk.Backend.Beta.Mediator.Result;

namespace Drydock.Application.Products.Queries.ProductGetById;

/// <summary>Represents a query to get a single product by id.</summary>
/// <param name="Id">Product id.</param>
public sealed record ProductGetByIdQuery(Guid Id)
    : IQuery<AppResult<ProductGetByIdResult.Success, ProductGetByIdResult.Failure>>;
