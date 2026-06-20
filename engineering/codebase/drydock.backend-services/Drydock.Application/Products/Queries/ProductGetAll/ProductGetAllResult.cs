using Drydock.Application.Products.Models;
using WoW.Two.Sdk.Backend.Beta.Mediator.Result;

namespace Drydock.Application.Products.Queries.ProductGetAll;

/// <summary>Outcome of listing all products.</summary>
public abstract record ProductGetAllResult
{
    private ProductGetAllResult() { }

    /// <summary>The products were listed.</summary>
    public sealed record Success(IReadOnlyList<ProductDto> Products) : ProductGetAllResult, ISuccessResult;

    /// <summary>The products could not be listed — <see cref="ICategorizedFailure.Category"/> maps the status.</summary>
    public sealed record Failure(string ErrorMessage, FailureCategory Category) : ProductGetAllResult, ICategorizedFailure;
}
