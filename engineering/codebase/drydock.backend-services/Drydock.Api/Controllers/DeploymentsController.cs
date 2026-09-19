using System.Text.Json;
using Drydock.Api.Requests;
using Drydock.Application.Deployments;
using Microsoft.AspNetCore.Mvc;
using WoW.Two.Sdk.Backend.Beta.Mediator;
using WoW.Two.Sdk.Backend.Beta.Mediator.Result;
using WoW.Two.Sdk.Backend.Beta.Web.Contracts;
using WoW.Two.Sdk.Backend.Beta.Web.ErrorMapping;

namespace Drydock.Api.Controllers;

/// <summary>Submits reviewed releases and observes target-owned deployment outcomes.</summary>
[ApiController]
[Route("api/deployments")]
public sealed class DeploymentsController(ISender sender, IErrorHttpStatusCodeMapper errors) : ControllerBase
{
    /// <summary>Lists provisioned deployment bindings.</summary>
    [HttpGet("targets")]
    public async Task<IActionResult> Targets(CancellationToken ct) =>
        Render(await sender.SendAsync(new DeploymentReadQuery("targets"), ct));

    /// <summary>Lists published deployment artifacts from approved repositories.</summary>
    [HttpGet("releases")]
    public async Task<IActionResult> Releases(CancellationToken ct) =>
        Render(await sender.SendAsync(new DeploymentReadQuery("releases"), ct));

    /// <summary>Reads the authoritative outcome from the target.</summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Status(Guid id, CancellationToken ct) =>
        Render(await sender.SendAsync(new DeploymentReadQuery("status", id.ToString()), ct));

    /// <summary>Queues a deployment; success is established by its later target status.</summary>
    [HttpPost]
    public async Task<IActionResult> Start(DeploymentStartRequest request, CancellationToken ct)
    {
        // A non-simple custom header blocks cross-origin cookie writes without trusting a body token.
        if (Request.Headers["X-Drydock-Action"] != "deploy")
            return Problem(statusCode: 400, detail: "An explicit deployment action is required.");
        var actor = User.Identity?.Name ?? User.FindFirst("wt:username")?.Value ?? "authenticated-admin";
        var result = await sender.SendAsync(new DeploymentStartCommand(request.Target, request.Release, actor), ct);
        return result.Match<IActionResult>(
            ok => AcceptedAtAction(nameof(Status), new { id = ok.Data.GetProperty("id").GetString() },
                ApiResponse<JsonElement>.Ok(ok.Data)),
            fail => Problem(statusCode: errors.ToStatusCode(fail.Error), detail: fail.Error.Message));
    }

    private IActionResult Render(AppResult<JsonElement> result) => result.Match<IActionResult>(
        ok => Ok(ApiResponse<JsonElement>.Ok(ok.Data)),
        fail => Problem(statusCode: errors.ToStatusCode(fail.Error), detail: fail.Error.Message));
}
