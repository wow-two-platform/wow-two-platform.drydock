using System.Text.Json;
using Wheelhouse.Application.Deployments;
using Microsoft.AspNetCore.Mvc;
using WoW.Two.Sdk.Backend.Beta.Mediator;
using WoW.Two.Sdk.Backend.Beta.Mediator.Result;
using WoW.Two.Sdk.Backend.Beta.Web.Contracts;
using WoW.Two.Sdk.Backend.Beta.Web.ErrorMapping;

namespace Wheelhouse.Api.Controllers;

/// <summary>Displays the fleet defined in reviewed source code.</summary>
[ApiController]
[Route("api/servers")]
public sealed class ServersController(ISender sender, IErrorHttpStatusCodeMapper errors) : ControllerBase
{
    /// <summary>Lists configured hosts without probing their reachability.</summary>
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken ct)
    {
        var result = await sender.SendAsync(new DeploymentReadQuery("servers"), ct);
        return result.Match<IActionResult>(
            ok => Ok(ApiResponse<JsonElement>.Ok(ok.Data)),
            fail => Problem(detail: fail.Error.Message, statusCode: errors.ToStatusCode(fail.Error)));
    }

    /// <summary>Rejects dynamic registration from older clients.</summary>
    [HttpPost]
    public IActionResult Create() => CodeOwned();

    /// <summary>Rejects dynamic removal from older clients.</summary>
    [HttpDelete("{id:guid}")]
    public IActionResult DeleteById(Guid id) => CodeOwned();

    private IActionResult CodeOwned() => Problem(statusCode: StatusCodes.Status405MethodNotAllowed,
        detail: "VPS integrations are defined in code. This inventory is read-only.");
}
