using Wheelhouse.Api.Requests;
using Wheelhouse.Application.Products.Commands.ProductCreate;
using Wheelhouse.Application.Products.Commands.ProductDelete;
using Wheelhouse.Application.Products.Commands.ProductUpdate;
using Wheelhouse.Application.Products.Models;
using Wheelhouse.Application.Products.Queries.ProductGetAll;
using Wheelhouse.Application.Products.Queries.ProductGetById;
using Wheelhouse.Application.Products.Queries.ProductVersionStatus;
using WoW.Two.Sdk.Backend.Beta.Web.Contracts;
using Microsoft.AspNetCore.Mvc;
using WoW.Two.Sdk.Backend.Beta.Mediator;
using WoW.Two.Sdk.Backend.Beta.Mediator.Result;
using WoW.Two.Sdk.Backend.Beta.Web.ErrorMapping;

namespace Wheelhouse.Api.Controllers;

/// <summary>Manages products.</summary>
[ApiController]
[Route("api/products")]
public sealed class ProductsController(ISender sender, IErrorHttpStatusCodeMapper errorMapper) : ControllerBase
{
    /// <summary>Gets all registered products.</summary>
    [HttpGet]
    [ProducesResponseType<ApiResponse<IReadOnlyList<ProductDto>>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Get(CancellationToken ct)
    {
        var result = await sender.SendAsync(new ProductGetAllQuery(), ct);

        return result.Match<IActionResult>(
            ok => Ok(ApiResponse<IReadOnlyList<ProductDto>>.Ok(ok.Data.Products)),
            fail => Problem(detail: fail.Error.Message, statusCode: errorMapper.ToStatusCode(fail.Error)));
    }

    /// <summary>Gets a product by id.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType<ApiResponse<ProductDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await sender.SendAsync(new ProductGetByIdQuery(id), ct);

        return result.Match<IActionResult>(
            ok => Ok(ApiResponse<ProductDto>.Ok(ok.Data.Product)),
            fail => Problem(detail: fail.Error.Message, statusCode: errorMapper.ToStatusCode(fail.Error)));
    }

    /// <summary>Gets a product's ready build/image status by id.</summary>
    [HttpGet("{id:guid}/version")]
    [ProducesResponseType<ApiResponse<ProductVersionDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetVersionById(Guid id, CancellationToken ct)
    {
        var result = await sender.SendAsync(new ProductVersionStatusQuery(id), ct);

        return result.Match<IActionResult>(
            ok => Ok(ApiResponse<ProductVersionDto>.Ok(ok.Data.Version)),
            fail => Problem(detail: fail.Error.Message, statusCode: errorMapper.ToStatusCode(fail.Error)));
    }

    /// <summary>Creates a product.</summary>
    [HttpPost]
    [ProducesResponseType<ApiResponse<ProductDto>>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create([FromBody] ProductCreateCommand command, CancellationToken ct)
    {
        var result = await sender.SendAsync(command, ct);

        return result.Match<IActionResult>(
            ok => CreatedAtAction(nameof(GetById), new { id = ok.Data.Product.Id }, ApiResponse<ProductDto>.Ok(ok.Data.Product)),
            fail => Problem(detail: fail.Error.Message, statusCode: errorMapper.ToStatusCode(fail.Error)));
    }

    /// <summary>Updates a product.</summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType<ApiResponse<ProductDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateById(Guid id, [FromBody] UpdateProductApiRequest request, CancellationToken ct)
    {
        var result = await sender.SendAsync(request.ToCommand(id), ct);

        return result.Match<IActionResult>(
            ok => Ok(ApiResponse<ProductDto>.Ok(ok.Data.Product)),
            fail => Problem(detail: fail.Error.Message, statusCode: errorMapper.ToStatusCode(fail.Error)));
    }

    /// <summary>Deletes a product.</summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteById(Guid id, CancellationToken ct)
    {
        var result = await sender.SendAsync(new ProductDeleteCommand(id), ct);

        return result.Match<IActionResult>(
            NoContent,
            fail => Problem(detail: fail.Error.Message, statusCode: errorMapper.ToStatusCode(fail.Error)));
    }
}
