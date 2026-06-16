using WoW.Two.Sdk.Backend.Beta.Mediator.Cqrs;
using WoW.Two.Sdk.Backend.Beta.Mediator.Result;

namespace Drydock.Application.Products.Queries.ProductGetAll;

/// <summary>Represents a query to get all products.</summary>
public sealed record ProductGetAllQuery
    : IQuery<AppResult<ProductGetAllResult.Success, ProductGetAllResult.Failure>>;
