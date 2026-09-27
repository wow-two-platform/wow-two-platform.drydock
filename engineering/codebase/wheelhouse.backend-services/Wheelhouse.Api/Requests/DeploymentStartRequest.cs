using System.ComponentModel.DataAnnotations;

namespace Wheelhouse.Api.Requests;

/// <summary>Selects trusted inventory entries; accepts no commands, credentials or paths.</summary>
/// <param name="Target">The code-owned target to deploy to.</param>
/// <param name="Release">The catalog entry to deploy.</param>
/// <param name="Confirm">The target ID typed out; required for prod on the local server and to skip the test pass.</param>
/// <param name="SkipTestPass">Deploys to prod a release that has not succeeded on test; needs <paramref name="Confirm"/>.</param>
public sealed record DeploymentStartRequest(
    [Required, RegularExpression("^[a-z][a-z0-9-]{0,47}$")] string Target,
    [Required, RegularExpression("^[a-z][a-z0-9-]{0,47}$")] string Release,
    [RegularExpression("^[a-z][a-z0-9-]{0,47}$")] string? Confirm = null,
    bool SkipTestPass = false);
