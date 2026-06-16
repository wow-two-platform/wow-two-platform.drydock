using System.Net;

namespace Drydock.Domain.Common;

/// <summary>Base API response envelope — holds shared constants only.</summary>
public record ApiResponse
{
    /// <summary>Title for the fall-through <c>Problem</c> of an unmatched result.</summary>
    public const string UnexpectedErrorMessage = "Unexpected error";
}

/// <summary>
/// Result-pattern HTTP success envelope — every 2xx body is an <see cref="ApiResponse{T}.Success"/> carrying the
/// typed payload under <c>.data</c>; errors never travel here (they go out as RFC-7807 ProblemDetails). A closed
/// union (private ctor + sealed cases) so the only instances are <see cref="Success"/> and <see cref="Failure"/>.
/// </summary>
/// <typeparam name="T">The payload type — always a DTO; the envelope never wraps another envelope.</typeparam>
public abstract record ApiResponse<T> : ApiResponse
{
    private ApiResponse() { }

    /// <summary>Successful response with the typed payload — the only shape a controller emits.</summary>
    public sealed record Success : ApiResponse<T>
    {
        /// <summary>Gets the response payload — serialized as <c>.data</c>.</summary>
        public required T Data { get; init; }
    }

    /// <summary>
    /// Failed response — client-side only, how an API client deserializes a non-2xx response so callers
    /// pattern-match instead of catching. Servers never emit this; that channel is ProblemDetails.
    /// </summary>
    public sealed record Failure : ApiResponse<T>
    {
        /// <summary>Gets the HTTP status code from the response.</summary>
        public required HttpStatusCode StatusCode { get; init; }

        /// <summary>Gets the error description (from ProblemDetails detail, body, or status reason).</summary>
        public required string Error { get; init; }
    }

    /// <summary>Wraps <paramref name="data"/> in a success envelope — the only way a controller builds a success body.</summary>
    public static Success Ok(T data) => new() { Data = data };
}
