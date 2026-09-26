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

    /// <summary>Reports service liveness, and whether this instance drives the local rehearsal rig instead of real hosts.</summary>
    [AllowAnonymous]
    [HttpGet("status")]
    public IActionResult Status()
    {
        return Ok(new { service = "Wheelhouse", status = "ok", localRig = deployment.IsLocalRig });
    }
}
