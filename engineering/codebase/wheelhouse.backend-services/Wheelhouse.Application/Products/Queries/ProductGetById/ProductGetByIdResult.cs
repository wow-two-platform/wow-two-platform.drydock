using Wheelhouse.Application.Products.Models;

namespace Wheelhouse.Application.Products.Queries.ProductGetById;

/// <summary>Success payload of fetching a single product — carried by the operation's <c>AppResult&lt;ProductGetByIdResult&gt;</c>; failures surface as an <c>AppError</c>.</summary>
/// <param name="Product">The product.</param>
public sealed record ProductGetByIdResult(ProductDto Product);
