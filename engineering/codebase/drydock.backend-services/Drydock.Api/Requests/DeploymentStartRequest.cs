using System.ComponentModel.DataAnnotations;

namespace Drydock.Api.Requests;

/// <summary>Selects trusted inventory entries; accepts no commands, credentials or paths.</summary>
public sealed record DeploymentStartRequest(
    [Required, RegularExpression("^[a-z][a-z0-9-]{0,47}$")] string Target,
    [Required, RegularExpression("^[a-z][a-z0-9-]{0,47}$")] string Release);
