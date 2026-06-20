using FluentValidation;

namespace Drydock.Application.Products.Commands.ProductCreate;

/// <summary>Validates <see cref="ProductCreateCommand"/>.</summary>
public sealed class ProductCreateCommandValidator : AbstractValidator<ProductCreateCommand>
{
    /// <summary>Configures the create-product field rules.</summary>
    public ProductCreateCommandValidator()
    {
        RuleFor(x => x.Slug)
            .NotEmpty();

        RuleFor(x => x.Name)
            .NotEmpty();

        RuleFor(x => x.Repo)
            .Must(ProductValidation.IsValidRepo)
            .WithMessage("Repo must be a 'owner/repo' reference (a single slash, no spaces, no scheme).");
    }
}
