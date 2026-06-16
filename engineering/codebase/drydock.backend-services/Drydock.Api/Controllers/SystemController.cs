using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Drydock.Api.Controllers;

/// <summary>System status endpoint for the dashboard header.</summary>
[ApiController]
[Route("api/system")]
public sealed class SystemController : ControllerBase
{
    /// <summary>Reports basic liveness and service identity. Anonymous — used by the sign-in screen before auth.</summary>
    [AllowAnonymous]
    [HttpGet("status")]
    public IActionResult Status()
    {
        return Ok(new { service = "Drydock", status = "ok" });
    }
}
