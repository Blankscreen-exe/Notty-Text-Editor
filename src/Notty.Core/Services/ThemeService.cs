using System.Text.Json;
using Notty.Core.Models;

namespace Notty.Core.Services;

/// <summary>
/// Discovers theme palettes as JSON files in a themes folder (default %AppData%\Notty\Themes),
/// seeding the built-in Light and Dark themes on first run. Invalid files are skipped, never fatal.
/// </summary>
public sealed class ThemeService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private readonly string _directory;

    public ThemeService(string? themesDirectory = null)
    {
        _directory = themesDirectory
            ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Notty", "Themes");
        System.IO.Directory.CreateDirectory(_directory);
    }

    public string Directory => _directory;

    /// <summary>Writes the built-in Light/Dark palettes to disk if they aren't present.</summary>
    public void EnsureDefaults()
    {
        SeedIfMissing("light.json", BuiltInLight);
        SeedIfMissing("dark.json", BuiltInDark);
    }

    /// <summary>Loads every valid *.json palette in the folder, sorted by display name.</summary>
    public IReadOnlyList<ThemePalette> LoadAll()
    {
        var palettes = new List<ThemePalette>();

        foreach (var file in System.IO.Directory.EnumerateFiles(_directory, "*.json"))
        {
            try
            {
                var palette = JsonSerializer.Deserialize<ThemePalette>(File.ReadAllText(file), JsonOptions);
                if (palette is null)
                    continue;
                if (string.IsNullOrWhiteSpace(palette.Name))
                    palette.Name = Path.GetFileNameWithoutExtension(file);
                palettes.Add(palette);
            }
            catch
            {
                // Skip malformed theme files rather than failing.
            }
        }

        if (palettes.Count == 0)
            palettes.Add(BuiltInLight);

        return palettes.OrderBy(p => p.Name, StringComparer.OrdinalIgnoreCase).ToList();
    }

    private void SeedIfMissing(string fileName, ThemePalette palette)
    {
        var path = Path.Combine(_directory, fileName);
        if (!File.Exists(path))
            File.WriteAllText(path, JsonSerializer.Serialize(palette, JsonOptions));
    }

    public static ThemePalette BuiltInLight => new()
    {
        Name = "Light",
        Base = "light",
        Colors = new()
        {
            ["Accent"] = "#2D6CDF",
            ["Window"] = "#FFFFFF",
            ["Panel"] = "#F4F5F7",
            ["Border"] = "#E1E3E8",
            ["Text"] = "#1F2328",
            ["MutedText"] = "#6B7280",
            ["Hover"] = "#ECEEF1",
            ["CodeBg"] = "#EEF1F4",
            ["CodeText"] = "#0B6E75",
        },
    };

    public static ThemePalette BuiltInDark => new()
    {
        Name = "Dark",
        Base = "dark",
        Colors = new()
        {
            ["Accent"] = "#4F8BF0",
            ["Window"] = "#1E1E1E",
            ["Panel"] = "#252526",
            ["Border"] = "#3A3A3D",
            ["Text"] = "#E6E6E6",
            ["MutedText"] = "#9AA0A6",
            ["Hover"] = "#2D2D30",
            ["CodeBg"] = "#2A2D2E",
            ["CodeText"] = "#4EC9B0",
        },
    };
}
