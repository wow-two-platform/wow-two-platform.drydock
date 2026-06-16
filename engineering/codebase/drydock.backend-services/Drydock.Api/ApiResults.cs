using Drydock.Domain.Results;

namespace Drydock.Api;

/// <summary>Maps a typed application failure to an HTTP status code — the single app-side error→status map.</summary>
internal static class ApiResults
{
    /// <summary>
    /// Translates a <see cref="FailureCategory"/> into an HTTP status code — a pure category→status map.
    /// The SDK marker carries no status; the mapping stays product-side. Callers pass the category off the
    /// product-side <see cref="IDrydockFailure"/> (e.g. <c>fail.Error.Category</c>).
    /// </summary>
    public static int ToStatusCode(FailureCategory category) => category switch
    {
        FailureCategory.Validation => StatusCodes.Status400BadRequest,
        FailureCategory.NotFound => StatusCodes.Status404NotFound,
        FailureCategory.Conflict => StatusCodes.Status409Conflict,
        FailureCategory.Unauthorized => StatusCodes.Status401Unauthorized,
        FailureCategory.Forbidden => StatusCodes.Status403Forbidden,
        FailureCategory.Unexpected => StatusCodes.Status500InternalServerError,
        _ => StatusCodes.Status500InternalServerError
    };
}
