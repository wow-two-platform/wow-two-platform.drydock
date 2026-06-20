using FluentValidation;

namespace Drydock.Application.Servers.Commands.ServerRegister;

/// <summary>Validates <see cref="ServerRegisterCommand"/>.</summary>
public sealed class ServerRegisterCommandValidator : AbstractValidator<ServerRegisterCommand>
{
    /// <summary>Configures the register-server field rules.</summary>
    public ServerRegisterCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty();

        RuleFor(x => x.Host)
            .NotEmpty();
    }
}
