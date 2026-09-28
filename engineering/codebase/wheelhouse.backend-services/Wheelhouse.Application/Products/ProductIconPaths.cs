using System.Text.RegularExpressions;

namespace Wheelhouse.Application.Products;

/// <summary>Chooses which file in a repository is the product's icon.</summary>
/// <remarks>
/// A favicon or icon under a web app's <c>public</c> folder wins, SVG before raster, a shallower path before a deeper
/// one. Build output, dependencies and tests never qualify.
/// </remarks>
public static partial class ProductIconPaths
{
    /// <summary>The largest icon Wheelhouse reads, in bytes.</summary>
    public const int MaxBytes = 256 * 1024;

    private static readonly string[] Excluded = ["node_modules/", "dist/", "bin/", "obj/", ".git/", "test/", "tests/", "stories/", "coverage/"];

    /// <summary>Returns the best icon path among <paramref name="paths"/>, or <see langword="null"/> when none qualifies.</summary>
    public static string? Choose(IEnumerable<string> paths) =>
        paths
            .Where(path => IconName().IsMatch(path) && !Excluded.Any(folder => path.Contains(folder, StringComparison.OrdinalIgnoreCase)))
            .OrderBy(Rank)
            .ThenBy(path => path.Count(character => character == '/'))
            .ThenBy(path => path, StringComparer.Ordinal)
            .FirstOrDefault();

    /// <summary>The media type for an icon path's extension.</summary>
    public static string ContentType(string path) =>
        Path.GetExtension(path).ToLowerInvariant() switch
        {
            ".svg" => "image/svg+xml",
            ".png" => "image/png",
            ".webp" => "image/webp",
            _ => "image/x-icon",
        };

    /// <summary>Lower ranks first: the file's name, then its format, then whether a web app serves it.</summary>
    private static int Rank(string path)
    {
        var name = Path.GetFileNameWithoutExtension(path).ToLowerInvariant();
        var extension = Path.GetExtension(path).ToLowerInvariant();
        var byName = name switch { "favicon" or "icon" => 0, "logo" => 1, "app-icon" => 2, _ => 3 };
        var byFormat = extension switch { ".svg" => 0, ".png" => 1, ".webp" => 2, _ => 3 };
        var byPlace = path.Contains("/public/", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("public/", StringComparison.OrdinalIgnoreCase) ? 0 : 1;
        return byName * 100 + byFormat * 10 + byPlace;
    }

    [GeneratedRegex(@"(^|/)(favicon|icon|logo|app-icon|apple-touch-icon)\.(svg|png|webp|ico)$", RegexOptions.IgnoreCase)]
    private static partial Regex IconName();
}
