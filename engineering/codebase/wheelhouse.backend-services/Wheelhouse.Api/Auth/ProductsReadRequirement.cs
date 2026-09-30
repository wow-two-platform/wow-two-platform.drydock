using Microsoft.AspNetCore.Authorization;

namespace Wheelhouse.Api.Auth;

/// <summary>Represents the requirement to read catalog products: the allowlisted operator, or an integration key that
/// grants <c>catalog:read</c>.</summary>
public sealed class ProductsReadRequirement : IAuthorizationRequirement;
