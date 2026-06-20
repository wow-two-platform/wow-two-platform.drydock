using FluentValidation;

namespace Drydock.Application.Products.Commands.ProductUpdate;

/// <summary>Validates <see cref="ProductUpdateCommand"/>.</summary>
public sealed class ProductUpdateCommandValidator : AbstractValidator<ProductUpdateCommand>
{
    /// <summary>Configures the update-product field rules.</summary>
    public ProductUpdateCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty();

        RuleFor(x => x.Repo)
            .Must(ProductValidation.IsValidRepo)
            .WithMessage("Repo must be a 'owner/repo' reference (a single slash, no spaces, no scheme).");
    }
}
