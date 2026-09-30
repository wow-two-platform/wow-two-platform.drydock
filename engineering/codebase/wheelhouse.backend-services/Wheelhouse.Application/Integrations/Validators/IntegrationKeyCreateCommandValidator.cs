using FluentValidation;
using Wheelhouse.Application.Integrations.Commands;
using Wheelhouse.Domain.Integrations.Constants;

namespace Wheelhouse.Application.Integrations.Validators;

/// <summary>Validates a request to create an integration key.</summary>
public sealed class IntegrationKeyCreateCommandValidator : AbstractValidator<IntegrationKeyCreateCommand>
{
    /// <summary>Configures the key-creation rules.</summary>
    public IntegrationKeyCreateCommandValidator()
    {
        // The name reaches the audit trail as `key:<name>`, so it stays short and plain.
        RuleFor(x => x.Name)
            .Matches("^[A-Za-z0-9][A-Za-z0-9 ._-]{0,59}$")
            .WithMessage("Name must be 1-60 letters, digits, spaces, dots, dashes or underscores.");

        RuleFor(x => x.Scopes)
            .NotEmpty()
            .WithMessage("Choose at least one scope.");

        RuleForEach(x => x.Scopes)
            .Must(scope => IntegrationScopeConstants.All.Contains(scope))
            .WithMessage("'{PropertyValue}' is not a scope Wheelhouse grants.");
    }
}
