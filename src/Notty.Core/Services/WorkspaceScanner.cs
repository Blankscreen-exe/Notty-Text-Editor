using Notty.Core.Models;

namespace Notty.Core.Services;

/// <summary>
/// Scans a workspace folder into a tree of <see cref="WorkspaceNode"/>.
/// Only supported note extensions are surfaced as file nodes; folders are always shown.
/// </summary>
public sealed class WorkspaceScanner
{
    public static readonly IReadOnlyList<string> SupportedExtensions = new[] { ".md", ".txt" };

    public static bool IsSupportedFile(string path) =>
        SupportedExtensions.Contains(Path.GetExtension(path), StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Builds the tree rooted at <paramref name="workspacePath"/>. Folders are listed
    /// before files, both sorted alphabetically. Inaccessible subfolders are skipped
    /// rather than aborting the whole scan.
    /// </summary>
    /// <param name="includeUnsupported">
    /// When true, files of any extension are shown (the UI marks them as unsupported); otherwise only
    /// <see cref="SupportedExtensions"/> are listed.
    /// </param>
    public WorkspaceNode Scan(string workspacePath, bool includeUnsupported = false)
    {
        if (!Directory.Exists(workspacePath))
            throw new DirectoryNotFoundException($"Workspace folder not found: {workspacePath}");

        var root = new WorkspaceNode
        {
            Name = new DirectoryInfo(workspacePath).Name,
            FullPath = workspacePath,
            IsDirectory = true,
        };
        PopulateChildren(root, includeUnsupported);
        return root;
    }

    private static void PopulateChildren(WorkspaceNode node, bool includeUnsupported)
    {
        IEnumerable<string> directories;
        IEnumerable<string> files;
        try
        {
            directories = Directory.EnumerateDirectories(node.FullPath);
            files = Directory.EnumerateFiles(node.FullPath);
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or IOException)
        {
            // Skip folders we cannot read instead of failing the whole scan.
            return;
        }

        foreach (var dir in directories.OrderBy(Path.GetFileName, StringComparer.OrdinalIgnoreCase))
        {
            var child = new WorkspaceNode
            {
                Name = Path.GetFileName(dir),
                FullPath = dir,
                IsDirectory = true,
            };
            PopulateChildren(child, includeUnsupported);
            node.Children.Add(child);
        }

        foreach (var file in files
                     .Where(f => includeUnsupported || IsSupportedFile(f))
                     .OrderBy(Path.GetFileName, StringComparer.OrdinalIgnoreCase))
        {
            node.Children.Add(new WorkspaceNode
            {
                Name = Path.GetFileName(file),
                FullPath = file,
                IsDirectory = false,
            });
        }
    }
}
