using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Wheelhouse.Application.Abstractions;
using Wheelhouse.Application.Products;
using WoW.Two.Sdk.Backend.Beta.Integrations;

namespace Wheelhouse.Infrastructure.Products;

/// <summary>Reads a product's icon from its GitHub repository with the signed-in operator's token, cached per repository.</summary>
/// <remarks>
/// Three REST reads: the default branch, the recursive tree and the chosen file's raw bytes. A missing or unreadable
/// icon is cached briefly, so the product falls back to its monogram without a GitHub call per page. Inline until
/// the SDK GitHub client reads repository trees and files.
/// </remarks>
public sealed partial class GitHubProductIconSource(
    IHttpClientFactory clients,
    IAccessTokenProvider tokens,
    IMemoryCache cache,
    ILogger<GitHubProductIconSource> logger) : IProductIconSource
{
    /// <summary>The named client, configured for the GitHub REST API.</summary>
    public const string ClientName = "github-files";

    private static readonly TimeSpan FoundFor = TimeSpan.FromHours(6);
    private static readonly TimeSpan MissingFor = TimeSpan.FromMinutes(30);

    /// <inheritdoc />
    public async Task<ProductIconImage?> FindAsync(string repository, CancellationToken ct)
    {
        var key = "product-icon:" + repository;
        if (cache.TryGetValue(key, out ProductIconImage? cached))
            return cached;

        var icon = await ReadAsync(repository, ct);
        cache.Set(key, icon, icon is null ? MissingFor : FoundFor);
        return icon;
    }

    private async Task<ProductIconImage?> ReadAsync(string repository, CancellationToken ct)
    {
        try
        {
            var client = clients.CreateClient(ClientName);
            var token = await tokens.GetAccessTokenAsync(ct);

            using var repo = await GetJsonAsync(client, token, $"repos/{repository}", ct);
            var branch = repo is not null && repo.RootElement.TryGetProperty("default_branch", out var name)
                ? name.GetString()
                : null;
            if (string.IsNullOrWhiteSpace(branch))
                return null;

            using var tree = await GetJsonAsync(
                client, token, $"repos/{repository}/git/trees/{Uri.EscapeDataString(branch)}?recursive=1", ct);
            if (tree is null || !tree.RootElement.TryGetProperty("tree", out var entries) || entries.ValueKind != JsonValueKind.Array)
                return null;

            var path = ProductIconPaths.Choose(entries.EnumerateArray()
                .Where(entry => entry.TryGetProperty("type", out var type) && type.GetString() == "blob"
                    && (!entry.TryGetProperty("size", out var size) || size.GetInt64() <= ProductIconPaths.MaxBytes)
                    && entry.TryGetProperty("path", out var item) && item.ValueKind == JsonValueKind.String)
                .Select(entry => entry.GetProperty("path").GetString()!));
            if (path is null)
                return null;

            using var request = Request(
                $"repos/{repository}/contents/{EscapePath(path)}?ref={Uri.EscapeDataString(branch)}", token, "application/vnd.github.raw");
            using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
            if (!response.IsSuccessStatusCode)
                return null;

            var bytes = await ReadBoundedAsync(response.Content, ct);
            return bytes is null ? null : new ProductIconImage(bytes, ProductIconPaths.ContentType(path), path);
        }
        catch (Exception exception) when (
            (exception is HttpRequestException or JsonException or TaskCanceledException) && !ct.IsCancellationRequested)
        {
            LogUnreadable(logger, repository, exception.GetType().Name);
            return null;
        }
    }

    private static async Task<JsonDocument?> GetJsonAsync(HttpClient client, string? token, string path, CancellationToken ct)
    {
        using var request = Request(path, token, "application/vnd.github+json");
        using var response = await client.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode)
            return null;
        await using var stream = await response.Content.ReadAsStreamAsync(ct);
        return await JsonDocument.ParseAsync(stream, cancellationToken: ct);
    }

    private static HttpRequestMessage Request(string path, string? token, string accept)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, path);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(accept));
        if (!string.IsNullOrWhiteSpace(token))
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return request;
    }

    /// <summary>Reads at most <see cref="ProductIconPaths.MaxBytes"/>; a larger file is no icon.</summary>
    private static async Task<byte[]?> ReadBoundedAsync(HttpContent content, CancellationToken ct)
    {
        await using var stream = await content.ReadAsStreamAsync(ct);
        using var buffer = new MemoryStream();
        var chunk = new byte[16 * 1024];
        int read;
        while ((read = await stream.ReadAsync(chunk, ct)) > 0)
        {
            buffer.Write(chunk, 0, read);
            if (buffer.Length > ProductIconPaths.MaxBytes)
                return null;
        }

        return buffer.ToArray();
    }

    private static string EscapePath(string path) => string.Join('/', path.Split('/').Select(Uri.EscapeDataString));

    [LoggerMessage(Level = LogLevel.Information, Message = "The icon of {Repository} could not be read ({Reason}).")]
    private static partial void LogUnreadable(ILogger logger, string repository, string reason);
}
