using FluentValidation;
using Wheelhouse.Application.Products.Commands;

namespace Wheelhouse.Application.Products.Validators;

/// <summary>Validates a request to record a catalog product's lifecycle.</summary>
public sealed class ProductLifecycleUpdateCommandValidator : AbstractValidator<ProductLifecycleUpdateCommand>
{
    /// <summary>Configures the lifecycle-update rules.</summary>
    public ProductLifecycleUpdateCommandValidator()
    {
        RuleFor(x => x.Slug)
            .Matches("^[a-z][a-z0-9-]{0,47}$")
            .WithMessage("Slug must be a catalog product slug.");

        RuleFor(x => x.Lifecycle)
            .IsInEnum();
    }
}
