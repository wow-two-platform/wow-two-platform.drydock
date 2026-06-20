using Drydock.Application.Products.Models;
using Drydock.Domain.Results;
using WoW.Two.Sdk.Backend.Beta.Mediator.Result;

namespace Drydock.Application.Products.Queries.ProductVersionStatus;

/// <summary>Represents the outcome of resolving a product's build/image status.</summary>
public abstract record ProductVersionStatusResult
{
    private ProductVersionStatusResult() { }

    /// <summary>The status was resolved.</summary>
    public sealed record Success(ProductVersionDto Version) : ProductVersionStatusResult, ISuccessResult;

    /// <summary>The status could not be returned — <see cref="IDrydockFailure.Category"/> maps the status.</summary>
    public sealed record Failure(string ErrorMessage, FailureCategory Category) : ProductVersionStatusResult, IDrydockFailure;
}
