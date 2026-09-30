using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Wheelhouse.Domain.Integrations.Constants;
using WoW.Two.Sdk.Backend.Beta.Identity.ApiKeys;
using WoW.Two.Sdk.Backend.Beta.Identity.Authorization;

namespace Wheelhouse.Api.Auth;

/// <summary>Admits a catalog read from the signed-in, allowlisted operator or from a key that grants
/// <c>catalog:read</c>; any other caller is refused.</summary>
/// <remarks>The operator check skips identities the key scheme authenticated, so a key's name never passes for an
/// allowlisted login.</remarks>
public sealed class ProductsReadAuthorizationHandler(AllowlistOptions allowlist)
    : AuthorizationHandler<ProductsReadRequirement>
{
    /// <inheritdoc />
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, ProductsReadRequirement requirement)
    {
        if (context.User.HasApiKeyScope(IntegrationScopeConstants.CatalogRead) || IsOperator(context.User))
            context.Succeed(requirement);
        return Task.CompletedTask;
    }

    /// <summary>Checks whether a session identity passes the allowlist; an empty allowlist admits any signed-in session.</summary>
    /// <param name="user">The principal the policy's schemes authenticated.</param>
    /// <returns><c>true</c> for the operator.</returns>
    private bool IsOperator(ClaimsPrincipal user)
    {
        var comparer = allowlist.CaseInsensitive ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;
        return user.Identities.Any(identity =>
            identity.IsAuthenticated
            && identity.AuthenticationType != ApiKeyAuthenticationDefaults.Scheme
            && (allowlist.Allowed.Count == 0
                || identity.FindAll(allowlist.ClaimType).Any(claim => allowlist.Allowed.Contains(claim.Value, comparer))));
    }
}
