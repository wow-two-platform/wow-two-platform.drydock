using AwesomeAssertions;
using Wheelhouse.Application.Products;

namespace Wheelhouse.Tests.Unit.Products;

/// <summary>Which repository file becomes the product's icon.</summary>
public sealed class ProductIconPathsTests
{
    [Fact]
    public void Choose_PrefersAWebAppFaviconAsSvg()
    {
        var chosen = ProductIconPaths.Choose([
            "docs/logo.png",
            "engineering/codebase/app.frontend-services/public/favicon.png",
            "engineering/codebase/app.frontend-services/public/favicon.svg",
            "README.md",
        ]);

        chosen.Should().Be("engineering/codebase/app.frontend-services/public/favicon.svg");
    }

    [Fact]
    public void Choose_IgnoresDependenciesBuildOutputAndTests()
    {
        var chosen = ProductIconPaths.Choose([
            "web/node_modules/pkg/favicon.svg",
            "web/dist/favicon.svg",
            "web/tests/fixtures/icon.svg",
            "brand/logo.svg",
        ]);

        chosen.Should().Be("brand/logo.svg");
    }

    [Fact]
    public void Choose_ReturnsNullWhenNothingLooksLikeAnIcon()
    {
        ProductIconPaths.Choose(["src/main.ts", "images/hero.png", "favicon.txt"]).Should().BeNull();
    }

    [Theory]
    [InlineData("a/favicon.svg", "image/svg+xml")]
    [InlineData("a/icon.png", "image/png")]
    [InlineData("a/logo.webp", "image/webp")]
    [InlineData("favicon.ico", "image/x-icon")]
    public void ContentType_FollowsTheExtension(string path, string expected)
    {
        ProductIconPaths.ContentType(path).Should().Be(expected);
    }
}
