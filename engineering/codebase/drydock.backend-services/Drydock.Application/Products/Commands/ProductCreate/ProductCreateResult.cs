using Drydock.Application.Products.Models;

namespace Drydock.Application.Products.Commands.ProductCreate;

/// <summary>Success payload of registering a product — carried by the operation's <c>AppResult&lt;ProductCreateResult&gt;</c>; failures surface as an <c>AppError</c>.</summary>
/// <param name="Product">The registered product.</param>
public sealed record ProductCreateResult(ProductDto Product);
