using System.Windows;
using System.Windows.Media;
using Notty.Core.Models;
using Notty.Core.Services;

namespace Notty.App.Services;

/// <summary>
/// Applies a <see cref="ThemePalette"/> to the live application by setting the brush resources that
/// the UI references via DynamicResource. Switching a theme updates every control instantly.
/// </summary>
public sealed class ThemeManager
{
    // Palette color slot -> the WPF brush resource key used throughout the XAML.
    private static readonly (string Key, string Brush)[] Map =
    {
        ("Accent", "AccentBrush"),
        ("Window", "WindowBackgroundBrush"),
        ("Panel", "PanelBackgroundBrush"),
        ("Border", "BorderBrushSoft"),
        ("Text", "TextBrush"),
        ("MutedText", "MutedTextBrush"),
        ("Hover", "HoverBrush"),
        ("CodeBg", "CodeBackgroundBrush"),
        ("CodeText", "CodeTextBrush"),
    };

    private readonly ThemeService _service;

    public ThemeManager(ThemeService service) => _service = service;

    public IReadOnlyList<ThemePalette> Available => _service.LoadAll();

    /// <summary>Applies the palette whose name matches (case-insensitively), or the first available.</summary>
    public ThemePalette ApplyByName(string? name)
    {
        var palettes = _service.LoadAll();
        var palette = palettes.FirstOrDefault(p => string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase))
            ?? palettes.FirstOrDefault()
            ?? ThemeService.BuiltInLight;

        Apply(palette);
        return palette;
    }

    public void Apply(ThemePalette palette)
    {
        // Fall back to the matching built-in (by Base) for any missing slot, so an older theme
        // file lacking newer keys (e.g. CodeBg) still gets sensible light/dark-appropriate colors.
        var fallback = string.Equals(palette.Base, "dark", StringComparison.OrdinalIgnoreCase)
            ? ThemeService.BuiltInDark.Colors
            : ThemeService.BuiltInLight.Colors;
        var resources = Application.Current.Resources;

        foreach (var (key, brushKey) in Map)
        {
            var hex = palette.Colors.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value)
                ? value
                : fallback[key];

            resources[brushKey] = CreateBrush(hex, fallback[key]);
        }
    }

    private static SolidColorBrush CreateBrush(string hex, string fallbackHex)
    {
        var color = TryParse(hex) ?? TryParse(fallbackHex) ?? Colors.Magenta;
        var brush = new SolidColorBrush(color);
        brush.Freeze();
        return brush;
    }

    private static Color? TryParse(string hex)
    {
        try
        {
            return (Color)ColorConverter.ConvertFromString(hex);
        }
        catch
        {
            return null;
        }
    }
}
