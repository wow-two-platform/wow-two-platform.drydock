using Drydock.Api.Requests;
using Drydock.Application.Servers.Commands.ServerRegister;
using Drydock.Application.Servers.Models;
using Drydock.Application.Servers.Queries.ServerGetAll;
using Drydock.Domain.Common;
using Microsoft.AspNetCore.Mvc;
using WoW.Two.Sdk.Backend.Beta.Mediator;
using WoW.Two.Sdk.Backend.Beta.Mediator.Result;

namespace Drydock.Api.Controllers;

/// <summary>Manages servers.</summary>
[ApiController]
[Route("api/servers")]
public sealed class ServersController(ISender sender) : ControllerBase
{
    /// <summary>Gets all registered servers.</summary>
    [HttpGet]
    [ProducesResponseType<ApiResponse<IReadOnlyList<ServerDto>>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Get(CancellationToken ct)
    {
        var result = await sender.SendAsync(new ServerGetAllQuery(), ct);

        return result.Match<ServerGetAllResult.Success, ServerGetAllResult.Failure, IActionResult>(
            ok => Ok(ApiResponse<IReadOnlyList<ServerDto>>.Ok(ok.Data.Servers)),
            fail => Problem(detail: fail.Error.ErrorMessage, statusCode: ApiResults.ToStatusCode(fail.Error.Category)));
    }

    /// <summary>Creates a server.</summary>
    [HttpPost]
    [ProducesResponseType<ApiResponse<ServerDto>>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create([FromBody] RegisterServerRequest request, CancellationToken ct)
    {
        var result = await sender.SendAsync(
            new ServerRegisterCommand(request.Name, request.Host, request.SshUser, request.SshPort, request.Region), ct);

        return result.Match<ServerRegisterResult.Success, ServerRegisterResult.Failure, IActionResult>(
            ok => CreatedAtAction(nameof(Get), new { id = ok.Data.Server.Id }, ApiResponse<ServerDto>.Ok(ok.Data.Server)),
            fail => Problem(detail: fail.Error.ErrorMessage, statusCode: ApiResults.ToStatusCode(fail.Error.Category)));
    }
}
