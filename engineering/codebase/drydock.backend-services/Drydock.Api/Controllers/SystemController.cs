using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Drydock.Api.Controllers;

/// <summary>Reports system status.</summary>
[ApiController]
[Route("api/system")]
public sealed class SystemController(Drydock.Persistence.DrydockDbContext database) : ControllerBase
{
    /// <summary>Reports database readiness for deployment health gates.</summary>
    [AllowAnonymous]
    [HttpGet("ready")]
    public async Task<IActionResult> Ready(CancellationToken ct) =>
        await database.Database.CanConnectAsync(ct) ? Ok() : StatusCode(503);

    /// <summary>Reports service liveness.</summary>
    [AllowAnonymous]
    [HttpGet("status")]
    public IActionResult Status()
    {
        return Ok(new { service = "Drydock", status = "ok" });
    }
}
