using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Wheelhouse.Infrastructure.Settings;

namespace Wheelhouse.Api.Controllers;

/// <summary>Reports system status.</summary>
[ApiController]
[Route("api/system")]
public sealed class SystemController(Wheelhouse.Persistence.WheelhouseDbContext database, DeploymentSettings deployment)
    : ControllerBase
{
    /// <summary>Reports database readiness for deployment health gates.</summary>
    [AllowAnonymous]
    [HttpGet("ready")]
    public async Task<IActionResult> Ready(CancellationToken ct) =>
        await database.Database.CanConnectAsync(ct) ? Ok() : StatusCode(503);

    /// <summary>The running build's informational version: the product version plus the commit it was built from.</summary>
    private static readonly string Version = typeof(SystemController).Assembly
        .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "unknown";

    /// <summary>Reports service liveness and version, and whether this instance drives the local rehearsal rig instead of real hosts.</summary>
    [AllowAnonymous]
    [HttpGet("status")]
    public IActionResult Status()
    {
        return Ok(new { service = "Wheelhouse", status = "ok", version = Version, localRig = deployment.IsLocalRig });
    }
}
