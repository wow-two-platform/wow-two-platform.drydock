using FluentValidation;

namespace Drydock.Application.Servers.Commands.ServerRegister;

/// <summary>Validates <see cref="ServerRegisterCommand"/>.</summary>
/// <remarks>
/// <c>SshUser</c> stays optional (blank → <c>root</c> downstream). The port and host rules reject only
/// <em>explicitly-invalid</em> input: an out-of-range port (use 1–65535) and a malformed host. An omitted port
/// (default <c>0</c>) is treated as "use the default 22" by the handler, so <c>0</c> is allowed here.
/// </remarks>
public sealed class ServerRegisterCommandValidator : AbstractValidator<ServerRegisterCommand>
{
    /// <summary>Configures the register-server field rules.</summary>
    public ServerRegisterCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty();

        RuleFor(x => x.Host)
            .NotEmpty()
            .Must(ServerValidation.IsValidHost)
            .WithMessage("Host must be a valid IP address or hostname (no scheme, no whitespace).");

        // 0 means "omitted" → the handler defaults it to 22. Any other out-of-range value is an explicit error.
        RuleFor(x => x.SshPort)
            .Must(port => port == 0 || ServerValidation.IsValidPort(port))
            .WithMessage($"SshPort must be between {ServerValidation.MinPort} and {ServerValidation.MaxPort}.");
    }
}
