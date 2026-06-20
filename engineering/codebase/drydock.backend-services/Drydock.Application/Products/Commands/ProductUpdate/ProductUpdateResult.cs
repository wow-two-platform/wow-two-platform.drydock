using Drydock.Application.Products.Models;
using WoW.Two.Sdk.Backend.Beta.Mediator.Result;

namespace Drydock.Application.Products.Commands.ProductUpdate;

/// <summary>Outcome of updating a product.</summary>
public abstract record ProductUpdateResult
{
    private ProductUpdateResult() { }

    /// <summary>The product was updated.</summary>
    public sealed record Success(ProductDto Product) : ProductUpdateResult, ISuccessResult;

    /// <summary>The product could not be updated — <see cref="ICategorizedFailure.Category"/> maps the status.</summary>
    public sealed record Failure(string ErrorMessage, FailureCategory Category) : ProductUpdateResult, ICategorizedFailure;
}
