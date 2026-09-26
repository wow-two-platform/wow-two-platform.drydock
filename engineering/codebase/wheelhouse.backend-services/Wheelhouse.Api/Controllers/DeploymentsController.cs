using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Text.Json;
using Wheelhouse.Api.Requests;
using Wheelhouse.Application.Deployments;
using Microsoft.AspNetCore.Mvc;
using WoW.Two.Sdk.Backend.Beta.Mediator;
using WoW.Two.Sdk.Backend.Beta.Mediator.Result;
using WoW.Two.Sdk.Backend.Beta.Web.Contracts;
using WoW.Two.Sdk.Backend.Beta.Web.ErrorMapping;

namespace Wheelhouse.Api.Controllers;

/// <summary>Submits reviewed releases and observes target-owned deployment outcomes.</summary>
[ApiController]
[Route("api/deployments")]
public sealed class DeploymentsController(ISender sender, IErrorHttpStatusCodeMapper errors) : ControllerBase
{
    // Route regex constraints ignore case; a validated parameter keeps catalog ids exact.
    private const string Slug = "^[a-z][a-z0-9-]{0,47}$";

    /// <summary>Lists recent deployments with their last observed outcome.</summary>
    [HttpGet]
    public async Task<IActionResult> History(CancellationToken ct) =>
        Render(await sender.SendAsync(new DeploymentReadQuery("jobs"), ct));

    /// <summary>Lists provisioned deployment bindings.</summary>
    [HttpGet("targets")]
    public async Task<IActionResult> Targets(CancellationToken ct) =>
        Render(await sender.SendAsync(new DeploymentReadQuery("targets"), ct));

    /// <summary>Reads the release a target runs and whether it needs reconciliation.</summary>
    [HttpGet("targets/{target}/state")]
    public async Task<IActionResult> State([RegularExpression(Slug)] string target, CancellationToken ct) =>
        Render(await sender.SendAsync(new DeploymentReadQuery("state", target), ct));

    /// <summary>Checks a target's readiness without changing it, optionally against one release.</summary>
    [HttpGet("targets/{target}/check")]
    public async Task<IActionResult> Check(
        [RegularExpression(Slug)] string target, [FromQuery, RegularExpression(Slug)] string? release, CancellationToken ct) =>
        Render(await sender.SendAsync(new DeploymentCheckQuery(target, release), ct));

    /// <summary>Acknowledges an interrupted or failed rollout the operator has inspected.</summary>
    [HttpPost("targets/{target}/reconcile")]
    public async Task<IActionResult> Reconcile(
        [RegularExpression(Slug)] string target, DeploymentReconcileRequest request, CancellationToken ct)
    {
        if (Request.Headers["X-Wheelhouse-Action"] != "reconcile")
            return Problem(statusCode: 400, detail: "An explicit reconcile action is required.");
        var result = await sender.SendAsync(new DeploymentReconcileCommand(target, request.Job!.Value.ToString(), Actor()), ct);
        return Render(result);
    }

    /// <summary>Summarizes deployment outcomes, rollout time and recovery time over the last days.</summary>
    [HttpGet("stats")]
    public async Task<IActionResult> Stats([FromQuery, Range(1, 90)] int days = 30, CancellationToken ct = default) =>
        Render(await sender.SendAsync(new DeploymentReadQuery("stats", days.ToString(CultureInfo.InvariantCulture)), ct));

    /// <summary>Reads every target's host and container vitals without changing them.</summary>
    [HttpGet("vitals")]
    public async Task<IActionResult> Vitals(CancellationToken ct) =>
        Render(await sender.SendAsync(new DeploymentReadQuery("vitals"), ct));

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
        if (Request.Headers["X-Wheelhouse-Action"] != "deploy")
            return Problem(statusCode: 400, detail: "An explicit deployment action is required.");
        var result = await sender.SendAsync(new DeploymentStartCommand(request.Target, request.Release, Actor()), ct);
        return result.Match<IActionResult>(
            ok => AcceptedAtAction(nameof(Status), new { id = ok.Data.GetProperty("id").GetString() },
                ApiResponse<JsonElement>.Ok(ok.Data)),
            fail => Problem(statusCode: errors.ToStatusCode(fail.Error), detail: fail.Error.Message));
    }

    private string Actor() => User.Identity?.Name ?? User.FindFirst("wt:username")?.Value ?? "authenticated-admin";

    private IActionResult Render(AppResult<JsonElement> result) => result.Match<IActionResult>(
        ok => Ok(ApiResponse<JsonElement>.Ok(ok.Data)),
        fail => Problem(statusCode: errors.ToStatusCode(fail.Error), detail: fail.Error.Message));
}
