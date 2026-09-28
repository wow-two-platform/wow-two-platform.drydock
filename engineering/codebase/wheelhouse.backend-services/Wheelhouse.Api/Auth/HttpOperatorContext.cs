using Wheelhouse.Application.Abstractions;

namespace Wheelhouse.Api.Auth;

/// <summary>Reads the operator from the signed-in principal, as the controllers name the actor.</summary>
public sealed class HttpOperatorContext(IHttpContextAccessor accessor) : IOperatorContext
{
    /// <inheritdoc />
    public string Actor
    {
        get
        {
            var user = accessor.HttpContext?.User;
            return user?.Identity?.Name ?? user?.FindFirst("wt:username")?.Value ?? "authenticated-admin";
        }
    }
}
