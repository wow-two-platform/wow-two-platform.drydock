using Drydock.Application.Products.Models;
using Drydock.Domain.Results;
using WoW.Two.Sdk.Backend.Beta.Mediator.Result;

namespace Drydock.Application.Products.Queries.ProductGetById;

/// <summary>Outcome of fetching a single product.</summary>
public abstract record ProductGetByIdResult
{
    private ProductGetByIdResult() { }

    /// <summary>The product was found.</summary>
    public sealed record Success(ProductDto Product) : ProductGetByIdResult, ISuccessResult;

    /// <summary>The product could not be returned — <see cref="IDrydockFailure.Category"/> maps the status.</summary>
    public sealed record Failure(string ErrorMessage, FailureCategory Category) : ProductGetByIdResult, IDrydockFailure;
}
