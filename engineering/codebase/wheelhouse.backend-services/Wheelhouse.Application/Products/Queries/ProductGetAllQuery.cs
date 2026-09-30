using Wheelhouse.Application.Products.Models;
using WoW.Two.Sdk.Backend.Beta.Mediator.Cqrs;
using WoW.Two.Sdk.Backend.Beta.Mediator.Result;

namespace Wheelhouse.Application.Products.Queries;

/// <summary>Represents a query to get every catalog product.</summary>
public sealed record ProductGetAllQuery : IQuery<AppResult<IReadOnlyList<ProductDto>>>;
