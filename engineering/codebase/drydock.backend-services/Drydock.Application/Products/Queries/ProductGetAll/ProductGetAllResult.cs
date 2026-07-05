using Drydock.Application.Products.Models;

namespace Drydock.Application.Products.Queries.ProductGetAll;

/// <summary>Success payload of listing all products — carried by the operation's <c>AppResult&lt;ProductGetAllResult&gt;</c>; failures surface as an <c>AppError</c>.</summary>
/// <param name="Products">The registered products.</param>
public sealed record ProductGetAllResult(IReadOnlyList<ProductDto> Products);
