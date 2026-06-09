using Drydock.Api.Requests;
using Drydock.Application.Servers.Commands.RegisterServer;
using Drydock.Application.Servers.Queries.ListServers;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Drydock.Api.Controllers;

/// <summary>Manages registered Hetzner VPS deploy targets.</summary>
[ApiController]
[Route("api/servers")]
public sealed class ServersController(ISender sender) : ControllerBase
{
    /// <summary>Lists all registered servers.</summary>
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct)
    {
        var result = await sender.Send(new ListServersQuery(), ct);
        return result.Match<IActionResult>(
            dtos => Ok(dtos),
            (error, message) => Problem(detail: message, statusCode: ApiResults.ToStatusCode(error)));
    }

    /// <summary>Registers a new server as a deploy target.</summary>
    [HttpPost]
    public async Task<IActionResult> Register([FromBody] RegisterServerRequest request, CancellationToken ct)
    {
        var command = new RegisterServerCommand(
            request.Name, request.Host, request.SshUser, request.SshPort, request.Region);

        var result = await sender.Send(command, ct);
        return result.Match<IActionResult>(
            dto => CreatedAtAction(nameof(List), new { id = dto.Id }, dto),
            (error, message) => Problem(detail: message, statusCode: ApiResults.ToStatusCode(error)));
    }
}
