using Drydock.Domain.Results;

namespace Drydock.Api;

/// <summary>Maps domain result error categories to HTTP status codes.</summary>
internal static class ApiResults
{
    /// <summary>Translates a <see cref="ResultError"/> into an HTTP status code.</summary>
    public static int ToStatusCode(ResultError error) => error switch
    {
        ResultError.NotFound => StatusCodes.Status404NotFound,
        ResultError.Conflict => StatusCodes.Status409Conflict,
        ResultError.Unauthorized => StatusCodes.Status401Unauthorized,
        ResultError.Forbidden => StatusCodes.Status403Forbidden,
        ResultError.Validation => StatusCodes.Status400BadRequest,
        _ => StatusCodes.Status500InternalServerError
    };
}
