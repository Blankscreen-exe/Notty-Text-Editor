namespace Notty.Core.Models;

/// <summary>
/// A node in the workspace tree: either a folder (category) or a supported note file.
/// This is a plain data model produced by <see cref="Services.WorkspaceScanner"/>;
/// the UI wraps it in a view model.
/// </summary>
public sealed class WorkspaceNode
{
    public required string Name { get; init; }

    public required string FullPath { get; init; }

    public required bool IsDirectory { get; init; }

    /// <summary>Child nodes for a directory. Empty for files.</summary>
    public List<WorkspaceNode> Children { get; } = new();
}
