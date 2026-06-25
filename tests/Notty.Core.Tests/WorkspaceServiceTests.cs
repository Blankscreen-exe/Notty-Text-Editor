using Notty.Core.Services;

namespace Notty.Core.Tests;

public sealed class WorkspaceServiceTests : IDisposable
{
    private readonly string _root;
    private readonly WorkspaceService _svc = new();

    public WorkspaceServiceTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "NottySvc_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_root);
    }

    [Fact]
    public void CreateNote_CreatesEmptyFileWithExtension()
    {
        var path = _svc.CreateNote(_root, "Ideas", ".md");

        Assert.True(File.Exists(path));
        Assert.Equal(".md", Path.GetExtension(path));
        Assert.Equal(string.Empty, File.ReadAllText(path));
    }

    [Fact]
    public void CreateNote_OnCollision_AppendsNumber()
    {
        var first = _svc.CreateNote(_root, "Note", ".txt");
        var second = _svc.CreateNote(_root, "Note", ".txt");

        Assert.NotEqual(first, second);
        Assert.EndsWith("Note (2).txt", second);
    }

    [Fact]
    public void CreateCategory_CreatesFolder()
    {
        var path = _svc.CreateCategory(_root, "Work");
        Assert.True(Directory.Exists(path));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("bad/name")]
    [InlineData("bad:name")]
    public void CreateNote_InvalidName_Throws(string name)
    {
        Assert.Throws<ArgumentException>(() => _svc.CreateNote(_root, name, ".md"));
    }

    [Fact]
    public void Rename_File_MovesToNewName()
    {
        var path = _svc.CreateNote(_root, "Old", ".md");

        var renamed = _svc.Rename(path, "New.md");

        Assert.False(File.Exists(path));
        Assert.True(File.Exists(renamed));
        Assert.EndsWith("New.md", renamed);
    }

    [Fact]
    public void Rename_Folder_MovesToNewName()
    {
        var folder = _svc.CreateCategory(_root, "Old");

        var renamed = _svc.Rename(folder, "New");

        Assert.True(Directory.Exists(renamed));
        Assert.EndsWith("New", renamed);
    }

    [Fact]
    public void Rename_ToExistingName_Throws()
    {
        _svc.CreateNote(_root, "Taken", ".md");
        var path = _svc.CreateNote(_root, "Mine", ".md");

        Assert.Throws<IOException>(() => _svc.Rename(path, "Taken.md"));
    }

    [Fact]
    public void Duplicate_CreatesCopyWithSuffix()
    {
        var path = _svc.CreateNote(_root, "Doc", ".txt");
        File.WriteAllText(path, "hello");

        var copy = _svc.Duplicate(path);

        Assert.True(File.Exists(copy));
        Assert.EndsWith("Doc (copy).txt", copy);
        Assert.Equal("hello", File.ReadAllText(copy));
    }

    public void Dispose()
    {
        try { Directory.Delete(_root, recursive: true); } catch { /* best effort */ }
    }
}
