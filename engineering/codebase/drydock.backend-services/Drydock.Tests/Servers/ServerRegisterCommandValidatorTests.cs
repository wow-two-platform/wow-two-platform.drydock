using AwesomeAssertions;
using Drydock.Application.Servers.Commands.ServerRegister;

namespace Drydock.Tests.Servers;

/// <summary>
/// Tests for <see cref="ServerRegisterCommandValidator"/>. Name + a well-formed Host are required; SshUser stays
/// optional (defaults downstream). Port <c>0</c> means "omitted" (handler defaults it to 22), but any other
/// out-of-range port is rejected, as is a malformed host — explicitly-invalid input no longer slips through.
/// </summary>
public sealed class ServerRegisterCommandValidatorTests
{
    private readonly ServerRegisterCommandValidator _validator = new();

    private static ServerRegisterCommand With(string name, string host, int sshPort = 0) =>
        new(Name: name, Host: host, SshUser: "", SshPort: sshPort, Region: null);

    [Fact]
    public void Valid_command_passes()
    {
        // Blank SshUser + omitted SshPort (0) are intentionally allowed (defaulted in the handler).
        var result = _validator.Validate(With("hetzner-fsn1", "10.0.0.1"));

        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void Empty_name_fails()
    {
        var result = _validator.Validate(With("", "10.0.0.1"));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(ServerRegisterCommand.Name));
    }

    [Fact]
    public void Empty_host_fails()
    {
        var result = _validator.Validate(With("hetzner-fsn1", ""));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(ServerRegisterCommand.Host));
    }

    // ---- SshPort range ----------------------------------------------------

    [Theory]
    [InlineData(0)]   // omitted → defaulted to 22 downstream
    [InlineData(1)]   // lower bound
    [InlineData(22)]  // canonical SSH port
    [InlineData(2222)]
    [InlineData(65535)] // upper bound
    public void Valid_or_omitted_port_passes(int port)
    {
        var result = _validator.Validate(With("hetzner-fsn1", "10.0.0.1", port));

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(65536)]
    [InlineData(99999)]
    public void Out_of_range_port_fails(int port)
    {
        var result = _validator.Validate(With("hetzner-fsn1", "10.0.0.1", port));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(ServerRegisterCommand.SshPort));
    }

    // ---- Host shape -------------------------------------------------------

    [Theory]
    [InlineData("10.0.0.1")]              // IPv4
    [InlineData("203.0.113.42")]
    [InlineData("hel1-prod")]             // bare hostname
    [InlineData("vps.example.com")]       // dotted DNS name
    [InlineData("a")]                     // single minimal label
    [InlineData("2001:db8::1")]           // IPv6
    public void Valid_host_passes(string host)
    {
        var result = _validator.Validate(With("hetzner-fsn1", host));

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("has space")]                       // whitespace
    [InlineData("http://example.com")]              // URL scheme
    [InlineData("https://10.0.0.1")]                // URL scheme
    [InlineData("bad_host")]                         // underscore not allowed in a label
    [InlineData("-leading.example.com")]            // label starts with hyphen
    [InlineData("trailing-.example.com")]           // label ends with hyphen
    [InlineData("double..dot")]                     // empty label
    public void Malformed_host_fails(string host)
    {
        var result = _validator.Validate(With("hetzner-fsn1", host));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(ServerRegisterCommand.Host));
    }
}
