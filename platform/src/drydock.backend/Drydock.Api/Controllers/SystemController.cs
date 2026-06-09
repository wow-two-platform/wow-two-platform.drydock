using Microsoft.AspNetCore.Mvc;

namespace Drydock.Api.Controllers;

/// <summary>System status endpoint for the dashboard header.</summary>
[ApiController]
[Route("api/system")]
public sealed class SystemController : ControllerBase
{
    /// <summary>Reports basic liveness and service identity.</summary>
    [HttpGet("status")]
    public IActionResult Status() => Ok(new { service = "Drydock", status = "ok" });
}
