using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Wheelhouse.Api.Auth;
using Wheelhouse.Api.Requests;
using Wheelhouse.Application.Products.Commands;
using Wheelhouse.Application.Products.Models;
using Wheelhouse.Application.Products.Queries;
using WoW.Two.Sdk.Backend.Beta.Mediator;
using WoW.Two.Sdk.Backend.Beta.Mediator.Result;
using WoW.Two.Sdk.Backend.Beta.Web.Contracts;
using WoW.Two.Sdk.Backend.Beta.Web.ErrorMapping;

namespace Wheelhouse.Api.Controllers;

/// <summary>Exposes the product catalog over HTTP.</summary>
[ApiController]
[Route("api/products")]
public sealed class ProductsController(ISender sender, IErrorHttpStatusCodeMapper errorMapper) : ControllerBase
{
    // Route regex constraints ignore case; a validated parameter keeps catalog slugs exact.
    private const string Slug = "^[a-z][a-z0-9-]{0,47}$";

    /// <summary>Gets every catalog product.</summary>
    [HttpGet]
    [Authorize(Policy = AuthConfigurationExtensions.ProductsReadPolicy)]
    [ProducesResponseType<ApiResponse<IReadOnlyList<ProductDto>>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Get(CancellationToken ct) =>
        (await sender.SendAsync(new ProductGetAllQuery(), ct)).Match<IActionResult>(
            ok => Ok(ApiResponse<IReadOnlyList<ProductDto>>.Ok(ok.Data)),
            fail => Problem(detail: fail.Error.Message, statusCode: errorMapper.ToStatusCode(fail.Error)));

    /// <summary>Gets one catalog product by its slug.</summary>
    [HttpGet("{slug}")]
    [Authorize(Policy = AuthConfigurationExtensions.ProductsReadPolicy)]
    [ProducesResponseType<ApiResponse<ProductDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetBySlug([RegularExpression(Slug)] string slug, CancellationToken ct) =>
        (await sender.SendAsync(new ProductGetBySlugQuery { Slug = slug }, ct)).Match<IActionResult>(
            ok => Ok(ApiResponse<ProductDto>.Ok(ok.Data)),
            fail => Problem(detail: fail.Error.Message, statusCode: errorMapper.ToStatusCode(fail.Error)));

    /// <summary>Gets the icon a catalog product's repository carries; not found when it carries none.</summary>
    [HttpGet("{slug}/icon")]
    [Authorize(Policy = AuthConfigurationExtensions.ProductsReadPolicy)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetIcon([RegularExpression(Slug)] string slug, CancellationToken ct) =>
        (await sender.SendAsync(new ProductIconQuery { Slug = slug }, ct)).Match<IActionResult>(
            ok =>
            {
                Response.Headers.CacheControl = "private, max-age=3600";
                Response.Headers.XContentTypeOptions = "nosniff";
                // An <img> ignores the disposition; opening the URL on its own downloads the file instead of
                // rendering it, so an SVG from a repository can never run script on this origin.
                return File(ok.Data.Content, ok.Data.ContentType, "icon" + Path.GetExtension(ok.Data.Path));
            },
            fail => Problem(detail: fail.Error.Message, statusCode: errorMapper.ToStatusCode(fail.Error)));

    /// <summary>Records where a catalog product stands in the portfolio.</summary>
    [HttpPut("{slug}/lifecycle")]
    [ProducesResponseType<ApiResponse<ProductDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateLifecycle(
        [RegularExpression(Slug)] string slug, UpdateProductLifecycleApiRequest request, CancellationToken ct)
    {
        // A non-simple custom header blocks cross-origin cookie writes without trusting a body token.
        if (Request.Headers["X-Wheelhouse-Action"] != "lifecycle")
            return Problem(statusCode: 400, detail: "An explicit lifecycle action is required.");
        var result = await sender.SendAsync(new ProductLifecycleUpdateCommand { Slug = slug, Lifecycle = request.Lifecycle }, ct);
        return result.Match<IActionResult>(
            ok => Ok(ApiResponse<ProductDto>.Ok(ok.Data)),
            fail => Problem(detail: fail.Error.Message, statusCode: errorMapper.ToStatusCode(fail.Error)));
    }
}
