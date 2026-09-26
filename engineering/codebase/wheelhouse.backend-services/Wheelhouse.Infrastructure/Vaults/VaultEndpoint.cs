using System.Text.Json;

namespace Wheelhouse.Infrastructure.Vaults;

/// <summary>One vault from the fleet catalog.</summary>
public sealed record VaultEndpoint(string Id, string Name, string ServerId, string Url)
{
    /// <summary>Maps a catalog entry emitted by the runner.</summary>
    public static VaultEndpoint From(JsonElement entry) => new(
        entry.GetProperty("id").GetString()!, entry.GetProperty("name").GetString()!,
        entry.GetProperty("serverId").GetString()!, entry.GetProperty("url").GetString()!.TrimEnd('/'));
}
