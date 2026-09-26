using Wheelhouse.Application.Products.Models;

namespace Wheelhouse.Application.Products.Commands.ProductUpdate;

/// <summary>Success payload of updating a product — carried by the operation's <c>AppResult&lt;ProductUpdateResult&gt;</c>; failures surface as an <c>AppError</c>.</summary>
/// <param name="Product">The updated product.</param>
public sealed record ProductUpdateResult(ProductDto Product);
