namespace Notty.Core.Models;

/// <summary>
/// A theme = a display name plus a set of named colors (hex strings). Stored as a small JSON file
/// in the themes folder. Adding a new palette file there makes a new theme available — no rebuild.
/// </summary>
public sealed class ThemePalette
{
    /// <summary>Display name shown in the menu/settings. Falls back to the file name if omitted.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Hint ("light" or "dark") for tooling; not required for rendering.</summary>
    public string Base { get; set; } = "light";

    /// <summary>Color slots, keyed by <see cref="ColorKeys"/> name, value is a hex string like "#2D6CDF".</summary>
    public Dictionary<string, string> Colors { get; set; } = new();

    /// <summary>The canonical color slots a complete theme defines.</summary>
    public static readonly IReadOnlyList<string> ColorKeys =
        new[] { "Accent", "Window", "Panel", "Border", "Text", "MutedText", "Hover", "CodeBg", "CodeText" };
}
