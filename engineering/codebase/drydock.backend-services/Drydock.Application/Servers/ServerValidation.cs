using System.Net;

namespace Drydock.Application.Servers;

/// <summary>Shared input checks for the Servers vertical — keeps the host/port rules in one place.</summary>
internal static class ServerValidation
{
    /// <summary>The lowest valid TCP port.</summary>
    public const int MinPort = 1;

    /// <summary>The highest valid TCP port.</summary>
    public const int MaxPort = 65535;

    /// <summary>Returns <see langword="true"/> when <paramref name="port"/> is in the valid TCP range (1–65535).</summary>
    public static bool IsValidPort(int port) => port is >= MinPort and <= MaxPort;

    /// <summary>
    /// Returns <see langword="true"/> when <paramref name="host"/> is a plausible IPv4/IPv6 address or DNS hostname —
    /// non-empty, no whitespace, no URL scheme, and either parses as an <see cref="IPAddress"/> or is a dotted/bare
    /// hostname whose labels are <c>[A-Za-z0-9-]</c> (not leading/trailing a hyphen). A reasonable shape gate, not a
    /// resolver — reachability is proven later over SSH.
    /// </summary>
    public static bool IsValidHost(string host)
    {
        if (string.IsNullOrWhiteSpace(host))
            return false;
        if (host.Any(char.IsWhiteSpace))
            return false;
        if (host.Contains("://", StringComparison.Ordinal))
            return false;

        // An IP literal (v4 or v6) is always acceptable.
        if (IPAddress.TryParse(host, out _))
            return true;

        // Otherwise require a DNS-style hostname: dot-separated labels, each [A-Za-z0-9-], no edge hyphens, ≤ 63 chars.
        var labels = host.Split('.');
        return labels.All(IsValidLabel);
    }

    private static bool IsValidLabel(string label) =>
        label.Length is > 0 and <= 63
        && label[0] != '-'
        && label[^1] != '-'
        && label.All(c => c is (>= 'a' and <= 'z') or (>= 'A' and <= 'Z') or (>= '0' and <= '9') or '-');
}
