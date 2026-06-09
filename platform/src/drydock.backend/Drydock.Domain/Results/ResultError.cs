namespace Drydock.Domain.Results;

/// <summary>Categorizes a failure so the API layer can map it to an HTTP status code.</summary>
public enum ResultError
{
    /// <summary>Unexpected/unclassified failure → 500.</summary>
    Unexpected = 0,

    /// <summary>Input failed validation → 400.</summary>
    Validation,

    /// <summary>Requested resource does not exist → 404.</summary>
    NotFound,

    /// <summary>Conflicts with current state, e.g. a duplicate → 409.</summary>
    Conflict,

    /// <summary>Authentication required or failed → 401.</summary>
    Unauthorized,

    /// <summary>Authenticated but not permitted → 403.</summary>
    Forbidden
}
