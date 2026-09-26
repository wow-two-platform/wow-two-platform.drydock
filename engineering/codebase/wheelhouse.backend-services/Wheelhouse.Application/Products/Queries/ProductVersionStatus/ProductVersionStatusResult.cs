using Wheelhouse.Application.Products.Models;

namespace Wheelhouse.Application.Products.Queries.ProductVersionStatus;

/// <summary>Success payload of resolving a product's build/image status — carried by the operation's <c>AppResult&lt;ProductVersionStatusResult&gt;</c>; failures surface as an <c>AppError</c>.</summary>
/// <param name="Version">The resolved build/image status.</param>
public sealed record ProductVersionStatusResult(ProductVersionDto Version);
