namespace Drydock.Application.Products.Commands.ProductDelete;

/// <summary>Success marker for deleting a product — no payload; the controller maps the success arm to <c>NoContent</c>. Failures surface as an <c>AppError</c>.</summary>
public sealed record ProductDeleteResult;
