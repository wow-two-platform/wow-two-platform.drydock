using WoW.Two.Sdk.Backend.Beta.Mediator.Result;

namespace Drydock.Application.Products.Commands.ProductDelete;

/// <summary>Outcome of deleting a product.</summary>
public abstract record ProductDeleteResult
{
    private ProductDeleteResult() { }

    /// <summary>The product was deleted — no payload, the controller maps it to <c>NoContent</c>.</summary>
    public sealed record Success : ProductDeleteResult, ISuccessResult;

    /// <summary>The product could not be deleted — <see cref="ICategorizedFailure.Category"/> maps the status.</summary>
    public sealed record Failure(string ErrorMessage, FailureCategory Category) : ProductDeleteResult, ICategorizedFailure;
}
