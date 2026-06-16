using WoW.Two.Sdk.Backend.Beta.Mediator.Cqrs;
using WoW.Two.Sdk.Backend.Beta.Mediator.Result;

namespace Drydock.Application.Products.Commands.ProductCreate;

/// <summary>Represents a command to register a new portfolio product.</summary>
/// <param name="Slug">URL-safe slug (unique, immutable).</param>
/// <param name="Name">Display name.</param>
/// <param name="Repo">The GitHub <c>{owner}/{repo}</c> that defines the product.</param>
public sealed record ProductCreateCommand(
    string Slug,
    string Name,
    string Repo) : ICommand<AppResult<ProductCreateResult.Success, ProductCreateResult.Failure>>;
