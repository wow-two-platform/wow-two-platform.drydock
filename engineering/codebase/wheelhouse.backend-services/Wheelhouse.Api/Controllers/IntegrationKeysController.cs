using Microsoft.AspNetCore.Mvc;
using Wheelhouse.Api.Requests;
using Wheelhouse.Application.Integrations.Commands;
using Wheelhouse.Application.Integrations.Models;
using Wheelhouse.Application.Integrations.Queries;
using WoW.Two.Sdk.Backend.Beta.Mediator;
using WoW.Two.Sdk.Backend.Beta.Mediator.Result;
using WoW.Two.Sdk.Backend.Beta.Web.Contracts;
using WoW.Two.Sdk.Backend.Beta.Web.ErrorMapping;

namespace Wheelhouse.Api.Controllers;

/// <summary>Exposes integration keys over HTTP.</summary>
[ApiController]
[Route("api/integration-keys")]
public sealed class IntegrationKeysController(ISender sender, IErrorHttpStatusCodeMapper errorMapper) : ControllerBase
{
    /// <summary>Gets every integration key, revoked ones included; never a secret.</summary>
    [HttpGet]
    [ProducesResponseType<ApiResponse<IReadOnlyList<IntegrationKeyDto>>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Get(CancellationToken ct) =>
        (await sender.SendAsync(new IntegrationKeyGetAllQuery(), ct)).Match<IActionResult>(
            ok => Ok(ApiResponse<IReadOnlyList<IntegrationKeyDto>>.Ok(ok.Data)),
            fail => Problem(detail: fail.Error.Message, statusCode: errorMapper.ToStatusCode(fail.Error)));

    /// <summary>Creates an integration key; the response carries its secret this once, never cached or stored.</summary>
    [HttpPost]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    [ProducesResponseType<ApiResponse<IntegrationKeyWithSecretDto>>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(CreateIntegrationKeyApiRequest request, CancellationToken ct)
    {
        if (Request.Headers["X-Wheelhouse-Action"] != "key-create")
            return Problem(statusCode: 400, detail: "An explicit key creation action is required.");
        var result = await sender.SendAsync(new IntegrationKeyCreateCommand { Name = request.Name, Scopes = request.Scopes }, ct);
        return result.Match<IActionResult>(
            ok => StatusCode(StatusCodes.Status201Created, ApiResponse<IntegrationKeyWithSecretDto>.Ok(ok.Data)),
            fail => Problem(detail: fail.Error.Message, statusCode: errorMapper.ToStatusCode(fail.Error)));
    }

    /// <summary>Revokes an integration key, so it never authenticates again.</summary>
    [HttpPost("{id:guid}/revoke")]
    [ProducesResponseType<ApiResponse<IntegrationKeyDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Revoke(Guid id, CancellationToken ct)
    {
        if (Request.Headers["X-Wheelhouse-Action"] != "key-revoke")
            return Problem(statusCode: 400, detail: "An explicit key revocation action is required.");
        var result = await sender.SendAsync(new IntegrationKeyRevokeCommand { Id = id }, ct);
        return result.Match<IActionResult>(
            ok => Ok(ApiResponse<IntegrationKeyDto>.Ok(ok.Data)),
            fail => Problem(detail: fail.Error.Message, statusCode: errorMapper.ToStatusCode(fail.Error)));
    }
}
