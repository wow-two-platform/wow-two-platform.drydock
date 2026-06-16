using WoW.Two.Sdk.Backend.Beta.Mediator.Cqrs;
using WoW.Two.Sdk.Backend.Beta.Mediator.Result;

namespace Drydock.Application.Products.Commands.ProductDelete;

/// <summary>Represents a command to delete a product by id.</summary>
/// <param name="Id">Product id.</param>
public sealed record ProductDeleteCommand(Guid Id)
    : ICommand<AppResult<ProductDeleteResult.Success, ProductDeleteResult.Failure>>;
