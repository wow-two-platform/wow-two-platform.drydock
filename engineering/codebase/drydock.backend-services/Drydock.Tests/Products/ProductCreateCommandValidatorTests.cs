using AwesomeAssertions;
using Drydock.Application.Products.Commands.ProductCreate;
using FluentValidation.Results;

namespace Drydock.Tests.Products;

/// <summary>
/// Tests for <see cref="ProductCreateCommandValidator"/>. The <c>Repo</c> rule delegates to the internal
/// <c>ProductValidation.IsValidRepo</c>, so this class doubles as the coverage for that <c>{owner}/{repo}</c>
/// format gate — varying only <c>Repo</c> (with a valid slug + name) makes each result a direct proxy for it.
/// </summary>
public sealed class ProductCreateCommandValidatorTests
{
    private readonly ProductCreateCommandValidator _validator = new();

    private static ProductCreateCommand With(string repo) => new(Slug: "drydock", Name: "Drydock", Repo: repo);

    private const string RepoError = nameof(ProductCreateCommand.Repo);

    // ---- IsValidRepo edges (via the Repo rule) ----------------------------

    [Theory]
    [InlineData("wow-two-platform/drydock")] // canonical owner/repo
    [InlineData("a/b")] // minimal non-empty both sides
    [InlineData("Owner123/repo.name-with_punct")] // dots/dashes/underscores are fine
    public void Valid_repo_passes(string repo)
    {
        var result = _validator.Validate(With(repo));

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("noslash")] // no slash
    [InlineData("")] // empty
    [InlineData("   ")] // whitespace-only
    [InlineData("owner repo")] // inner whitespace (and no slash)
    [InlineData("ow ner/repo")] // whitespace inside a part
    [InlineData("http://github.com/owner/repo")] // URL scheme rejected
    [InlineData("https://github.com/owner/repo")] // URL scheme rejected
    [InlineData("/repo")] // leading slash → empty owner
    [InlineData("owner/")] // trailing slash → empty repo
    [InlineData("owner/repo/extra")] // more than one slash
    public void Invalid_repo_fails_on_the_repo_rule(string repo)
    {
        var result = _validator.Validate(With(repo));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == RepoError);
    }

    // ---- the other field rules --------------------------------------------

    [Fact]
    public void Empty_slug_fails()
    {
        var result = _validator.Validate(new ProductCreateCommand(Slug: "", Name: "Drydock", Repo: "owner/repo"));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(ProductCreateCommand.Slug));
    }

    [Fact]
    public void Empty_name_fails()
    {
        var result = _validator.Validate(new ProductCreateCommand(Slug: "drydock", Name: "", Repo: "owner/repo"));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(ProductCreateCommand.Name));
    }

    [Fact]
    public void Fully_valid_command_passes()
    {
        ValidationResult result = _validator.Validate(With("owner/repo"));

        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }
}
