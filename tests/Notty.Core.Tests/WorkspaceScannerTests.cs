using Notty.Core.Services;

namespace Notty.Core.Tests;

public sealed class WorkspaceScannerTests : IDisposable
{
    private readonly string _root;

    public WorkspaceScannerTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "NottyTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_root);
    }

    [Fact]
    public void Scan_ListsFoldersAndSupportedFilesOnly()
    {
        Directory.CreateDirectory(Path.Combine(_root, "Work"));
        File.WriteAllText(Path.Combine(_root, "Work", "Plan.md"), "x");
        File.WriteAllText(Path.Combine(_root, "Shopping.txt"), "x");
        File.WriteAllText(Path.Combine(_root, "ignore.pdf"), "x");

        var tree = new WorkspaceScanner().Scan(_root);

        var names = tree.Children.Select(c => c.Name).ToList();
        Assert.Contains("Work", names);
        Assert.Contains("Shopping.txt", names);
        Assert.DoesNotContain("ignore.pdf", names);

        var work = tree.Children.Single(c => c.Name == "Work");
        Assert.True(work.IsDirectory);
        Assert.Contains(work.Children, c => c.Name == "Plan.md");
    }

    [Fact]
    public void Scan_OrdersFoldersBeforeFiles()
    {
        Directory.CreateDirectory(Path.Combine(_root, "Zeta"));
        File.WriteAllText(Path.Combine(_root, "Alpha.md"), "x");

        var tree = new WorkspaceScanner().Scan(_root);

        Assert.True(tree.Children[0].IsDirectory);
        Assert.Equal("Zeta", tree.Children[0].Name);
        Assert.Equal("Alpha.md", tree.Children[1].Name);
    }

    [Fact]
    public void Scan_MissingWorkspace_Throws()
    {
        Assert.Throws<DirectoryNotFoundException>(
            () => new WorkspaceScanner().Scan(Path.Combine(_root, "nope")));
    }

    [Fact]
    public void Scan_IncludeUnsupported_ShowsOtherFileTypes()
    {
        File.WriteAllText(Path.Combine(_root, "notes.md"), "x");
        File.WriteAllText(Path.Combine(_root, "data.pdf"), "x");

        var defaultScan = new WorkspaceScanner().Scan(_root);
        Assert.DoesNotContain(defaultScan.Children, c => c.Name == "data.pdf");

        var withUnsupported = new WorkspaceScanner().Scan(_root, includeUnsupported: true);
        Assert.Contains(withUnsupported.Children, c => c.Name == "data.pdf");
        Assert.Contains(withUnsupported.Children, c => c.Name == "notes.md");
    }

    public void Dispose()
    {
        try { Directory.Delete(_root, recursive: true); } catch { /* best effort */ }
    }
}
