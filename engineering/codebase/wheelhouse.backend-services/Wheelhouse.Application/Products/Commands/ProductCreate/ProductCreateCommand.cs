using Wheelhouse.Application.Audit;
using WoW.Two.Sdk.Backend.Beta.Mediator.Cqrs;
using WoW.Two.Sdk.Backend.Beta.Mediator.Result;

namespace Wheelhouse.Application.Products.Commands.ProductCreate;

/// <summary>Represents a command to register a new portfolio product.</summary>
/// <param name="Slug">URL-safe slug (unique, immutable).</param>
/// <param name="Name">Display name.</param>
/// <param name="Repo">The GitHub <c>{owner}/{repo}</c> that defines the product.</param>
public sealed record ProductCreateCommand(
    string Slug,
    string Name,
    string Repo) : ICommand<AppResult<ProductCreateResult>>, IAuditedCommand
{
    /// <inheritdoc />
    public string AuditAction => "product.create";

    /// <inheritdoc />
    public string AuditSubject => Slug;

    /// <inheritdoc />
    public string AuditDetail => Name + " (" + Repo + ")";
}
