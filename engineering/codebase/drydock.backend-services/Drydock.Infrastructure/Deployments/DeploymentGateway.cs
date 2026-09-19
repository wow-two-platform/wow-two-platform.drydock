using System.Diagnostics;
using System.Text.Json;
using Drydock.Application.Abstractions;
using Drydock.Infrastructure.Settings;
using WoW.Two.Sdk.Backend.Beta.Foundation.Errors;
using WoW.Two.Sdk.Backend.Beta.Mediator.Result;

namespace Drydock.Infrastructure.Deployments;

/// <summary>Runs the same bounded SSH adapter used by operator recovery.</summary>
public sealed class DeploymentGateway(DeploymentSettings settings) : IDeploymentGateway
{
    /// <inheritdoc />
    public Task<AppResult<JsonElement>> ReadAsync(string resource, string? id, CancellationToken ct) =>
        resource switch
        {
            "targets" or "releases" => RunAsync([resource], ct),
            "status" when Guid.TryParse(id, out _) => RunAsync(["status", "--job", id], ct),
            _ => Task.FromResult(AppResult<JsonElement>.Fail(AppErrors.NotFound("Unknown deployment resource.")))
        };

    /// <inheritdoc />
    public Task<AppResult<JsonElement>> StartAsync(string target, string release, string actor, CancellationToken ct) =>
        RunAsync(["submit", "--target", target, "--bundle", release, "--actor", actor], ct);

    private async Task<AppResult<JsonElement>> RunAsync(string[] arguments, CancellationToken ct)
    {
        if (!File.Exists(settings.TransportPath))
            return AppResult<JsonElement>.Fail(AppErrors.Unexpected("Deployment runner is not installed."));

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeout.CancelAfter(TimeSpan.FromSeconds(110));
        using var process = new Process
        {
            StartInfo = new ProcessStartInfo(settings.Python)
            {
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            }
        };
        process.StartInfo.ArgumentList.Add(settings.TransportPath);
        foreach (var argument in arguments)
            process.StartInfo.ArgumentList.Add(argument);
        process.StartInfo.ArgumentList.Add("--root");
        process.StartInfo.ArgumentList.Add(settings.Root);
        try
        {
            process.Start();
            var output = process.StandardOutput.ReadToEndAsync(timeout.Token);
            var error = process.StandardError.ReadToEndAsync(timeout.Token);
            await process.WaitForExitAsync(timeout.Token);
            await error;
            if (process.ExitCode != 0)
                return AppResult<JsonElement>.Fail(AppErrors.Unexpected("Deployment operation failed. Inspect the target privately."));
            using var document = JsonDocument.Parse(await output);
            return AppResult<JsonElement>.Ok(document.RootElement.Clone());
        }
        catch (OperationCanceledException)
        {
            if (!process.HasExited) process.Kill(entireProcessTree: true);
            return AppResult<JsonElement>.Fail(AppErrors.Unexpected("Deployment response timed out. Reconcile target state before retrying."));
        }
        catch (Exception exception) when (exception is System.ComponentModel.Win32Exception or JsonException or IOException)
        {
            return AppResult<JsonElement>.Fail(AppErrors.Unexpected("Deployment runner is unavailable."));
        }
    }
}
