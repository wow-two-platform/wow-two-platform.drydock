using Drydock.Application.Products.Models;
using WoW.Two.Sdk.Backend.Beta.Mediator.Result;

namespace Drydock.Application.Products.Commands.ProductCreate;

/// <summary>Outcome of registering a product.</summary>
public abstract record ProductCreateResult
{
    private ProductCreateResult() { }

    /// <summary>The product was registered.</summary>
    public sealed record Success(ProductDto Product) : ProductCreateResult, ISuccessResult;

    /// <summary>The product could not be registered — <see cref="ICategorizedFailure.Category"/> maps the status.</summary>
    public sealed record Failure(string ErrorMessage, FailureCategory Category) : ProductCreateResult, ICategorizedFailure;
}
