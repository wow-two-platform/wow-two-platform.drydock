using Drydock.Application.Servers.Commands.ServerDelete;
using Drydock.Application.Servers.Commands.ServerRegister;
using Drydock.Application.Servers.Models;
using Drydock.Application.Servers.Queries.ServerGetAll;
using WoW.Two.Sdk.Backend.Beta.Web.Contracts;
using Microsoft.AspNetCore.Mvc;
using WoW.Two.Sdk.Backend.Beta.Mediator;
using WoW.Two.Sdk.Backend.Beta.Mediator.Result;
using WoW.Two.Sdk.Backend.Beta.Web.Results;

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

        return result.Match<IActionResult>(
            ok => Ok(ApiResponse<IReadOnlyList<ServerDto>>.Ok(ok.Data.Servers)),
            fail => Problem(detail: fail.Error.ErrorMessage, statusCode: fail.Error.Category.ToStatusCode()));
    }

    /// <summary>Creates a server.</summary>
    [HttpPost]
    [ProducesResponseType<ApiResponse<ServerDto>>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create([FromBody] ServerRegisterCommand command, CancellationToken ct)
    {
        var result = await sender.SendAsync(command, ct);

        return result.Match<IActionResult>(
            ok => CreatedAtAction(nameof(Get), new { id = ok.Data.Server.Id }, ApiResponse<ServerDto>.Ok(ok.Data.Server)),
            fail => Problem(detail: fail.Error.ErrorMessage, statusCode: fail.Error.Category.ToStatusCode()));
    }

    /// <summary>Deletes a server.</summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteById(Guid id, CancellationToken ct)
    {
        var result = await sender.SendAsync(new ServerDeleteCommand(id), ct);

        return result.Match<IActionResult>(
            NoContent,
            fail => Problem(detail: fail.Error.ErrorMessage, statusCode: fail.Error.Category.ToStatusCode()));
    }
}
