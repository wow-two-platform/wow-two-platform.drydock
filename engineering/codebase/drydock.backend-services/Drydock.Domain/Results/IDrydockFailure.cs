using WoW.Two.Sdk.Backend.Beta.Mediator.Result;

namespace Drydock.Domain.Results;

/// <summary>
/// Product-side failure shape every operation's <c>Failure</c> result implements. Refines the SDK's empty
/// <see cref="IFailureResult"/> marker with the two fields the API layer needs: a human-readable message for
/// the ProblemDetails body and a <see cref="FailureCategory"/> the status map keys on. The failure→HTTP
/// mapping stays product-side — <c>ApiResults.ToStatusCode</c> branches on this, the SDK marker carries no status.
/// </summary>
public interface IDrydockFailure : IFailureResult
{
    /// <summary>Gets the human-readable error message surfaced as the ProblemDetails detail.</summary>
    string ErrorMessage { get; }

    /// <summary>Gets the failure category that drives the HTTP status code.</summary>
    FailureCategory Category { get; }
}
