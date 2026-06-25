using Notty.Core.Services;

namespace Notty.Core.Tests;

public sealed class ThemeServiceTests : IDisposable
{
    private readonly string _dir;

    public ThemeServiceTests()
    {
        _dir = Path.Combine(Path.GetTempPath(), "NottyThemes_" + Guid.NewGuid().ToString("N"));
    }

    [Fact]
    public void EnsureDefaults_SeedsLightAndDark()
    {
        var svc = new ThemeService(_dir);
        svc.EnsureDefaults();

        Assert.True(File.Exists(Path.Combine(_dir, "light.json")));
        Assert.True(File.Exists(Path.Combine(_dir, "dark.json")));

        var names = svc.LoadAll().Select(p => p.Name).ToList();
        Assert.Contains("Light", names);
        Assert.Contains("Dark", names);
    }

    [Fact]
    public void LoadAll_SkipsMalformedFiles()
    {
        var svc = new ThemeService(_dir);
        svc.EnsureDefaults();
        File.WriteAllText(Path.Combine(_dir, "broken.json"), "{ not valid json");

        var palettes = svc.LoadAll();

        Assert.DoesNotContain(palettes, p => p.Name == "broken");
        Assert.Contains(palettes, p => p.Name == "Light");
    }

    [Fact]
    public void LoadAll_UsesFileNameWhenNameMissing()
    {
        var svc = new ThemeService(_dir);
        File.WriteAllText(Path.Combine(_dir, "ocean.json"),
            "{ \"colors\": { \"Accent\": \"#3BA7C4\" } }");

        var palette = svc.LoadAll().Single(p => p.Name == "ocean");
        Assert.Equal("#3BA7C4", palette.Colors["Accent"]);
    }

    public void Dispose()
    {
        try { System.IO.Directory.Delete(_dir, recursive: true); } catch { /* best effort */ }
    }
}
