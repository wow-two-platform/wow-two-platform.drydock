namespace Wheelhouse.Application.Servers.Commands.ServerDelete;

/// <summary>Success marker for deleting a server — no payload; the controller maps the success arm to <c>NoContent</c>. Failures surface as an <c>AppError</c>.</summary>
public sealed record ServerDeleteResult;
