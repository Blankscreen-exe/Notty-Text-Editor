using System.Text.Json;
using Notty.Core.Models;

namespace Notty.Core.Services;

/// <summary>
/// Loads and persists <see cref="AppSettings"/> as JSON under %AppData%\Notty.
/// Never throws on read: a missing or corrupt file yields fresh defaults.
/// </summary>
public sealed class SettingsService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private readonly string _settingsPath;

    public SettingsService(string? settingsDirectory = null)
    {
        var dir = settingsDirectory
            ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Notty");
        Directory.CreateDirectory(dir);
        _settingsPath = Path.Combine(dir, "settings.json");
    }

    public string SettingsPath => _settingsPath;

    public AppSettings Load()
    {
        try
        {
            if (!File.Exists(_settingsPath))
                return new AppSettings();

            var json = File.ReadAllText(_settingsPath);
            return JsonSerializer.Deserialize<AppSettings>(json, JsonOptions) ?? new AppSettings();
        }
        catch
        {
            // Corrupt or unreadable settings should never block startup.
            return new AppSettings();
        }
    }

    public void Save(AppSettings settings)
    {
        var json = JsonSerializer.Serialize(settings, JsonOptions);
        File.WriteAllText(_settingsPath, json);
    }

    /// <summary>
    /// Records <paramref name="filePath"/> as the most recently opened document,
    /// de-duplicating and trimming to <see cref="AppSettings.MaxRecentDocuments"/>.
    /// </summary>
    public void AddRecentDocument(AppSettings settings, string filePath, DateTimeOffset openedAt)
    {
        settings.RecentDocuments.RemoveAll(r =>
            string.Equals(r.FilePath, filePath, StringComparison.OrdinalIgnoreCase));

        settings.RecentDocuments.Insert(0, new RecentDocument
        {
            FilePath = filePath,
            LastOpened = openedAt,
        });

        if (settings.RecentDocuments.Count > AppSettings.MaxRecentDocuments)
        {
            settings.RecentDocuments.RemoveRange(
                AppSettings.MaxRecentDocuments,
                settings.RecentDocuments.Count - AppSettings.MaxRecentDocuments);
        }
    }
}
