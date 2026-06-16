using Drydock.Domain.Products.Enums;
using WoW.Two.Sdk.Backend.Beta.Mediator.Cqrs;
using WoW.Two.Sdk.Backend.Beta.Mediator.Result;

namespace Drydock.Application.Products.Commands.ProductUpdate;

/// <summary>Represents a command to update a product's mutable fields (slug is immutable).</summary>
/// <param name="Id">Product id.</param>
/// <param name="Name">Display name.</param>
/// <param name="Repo">The GitHub <c>{owner}/{repo}</c> that defines the product.</param>
/// <param name="Status">Lifecycle state.</param>
public sealed record ProductUpdateCommand(
    Guid Id,
    string Name,
    string Repo,
    ProductStatus Status) : ICommand<AppResult<ProductUpdateResult.Success, ProductUpdateResult.Failure>>;
