using AwesomeAssertions;
using Wheelhouse.Application.Products.Commands;
using Wheelhouse.Application.Products.Validators;
using Wheelhouse.Domain.Products.Enums;

namespace Wheelhouse.Tests.Unit.Products;

/// <summary>Tests for <see cref="ProductLifecycleUpdateCommandValidator"/>.</summary>
public sealed class ProductLifecycleUpdateCommandValidatorTests
{
    private readonly ProductLifecycleUpdateCommandValidator _validator = new();

    [Theory]
    [InlineData("foreverpin")]
    [InlineData("a")]
    [InlineData("smart-qr-2")]
    public void Validate_ShouldPass_WhenSlugIsACatalogSlug(string slug)
    {
        var result = _validator.Validate(new ProductLifecycleUpdateCommand { Slug = slug, Lifecycle = ProductLifecycle.Live });

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("ForeverPin")]
    [InlineData("2fast")]
    [InlineData("-pin")]
    [InlineData("foreverpin/../x")]
    public void Validate_ShouldFail_WhenSlugIsNotACatalogSlug(string slug)
    {
        var result = _validator.Validate(new ProductLifecycleUpdateCommand { Slug = slug, Lifecycle = ProductLifecycle.Live });

        result.Errors.Should().ContainSingle(error => error.PropertyName == nameof(ProductLifecycleUpdateCommand.Slug));
    }

    [Fact]
    public void Validate_ShouldFail_WhenLifecycleIsOutsideTheSet()
    {
        var result = _validator.Validate(new ProductLifecycleUpdateCommand { Slug = "foreverpin", Lifecycle = (ProductLifecycle)42 });

        result.Errors.Should().ContainSingle(error => error.PropertyName == nameof(ProductLifecycleUpdateCommand.Lifecycle));
    }
}
