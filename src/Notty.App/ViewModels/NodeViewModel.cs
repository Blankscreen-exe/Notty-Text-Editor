using System.Collections.ObjectModel;
using System.IO;
using Notty.App.Mvvm;
using Notty.Core.Models;
using Notty.Core.Services;

namespace Notty.App.ViewModels;

/// <summary>View model wrapping a <see cref="WorkspaceNode"/> for display in the folder tree.</summary>
public sealed class NodeViewModel : ObservableObject
{
    // Icon SVGs, embedded as resources. Swap the .svg files under Assets\Icons and rebuild.
    private const string IconBase = "pack://application:,,,/Assets/Icons/";
    private static readonly Uri FolderIcon = new(IconBase + "folders/folder.svg");
    private static readonly Uri MarkdownIcon = new(IconBase + "files/markdown.svg");
    private static readonly Uri TextIcon = new(IconBase + "files/text.svg");
    private static readonly Uri UnknownIcon = new(IconBase + "files/unknown.svg");

    private bool _isExpanded;
    private bool _isSelected;

    /// <param name="isDoor">
    /// True for a folder beyond the inline depth cap: shown but not expanded; clicking it re-roots the tree.
    /// </param>
    public NodeViewModel(WorkspaceNode model, bool isDoor = false)
    {
        Name = model.Name;
        FullPath = model.FullPath;
        IsDirectory = model.IsDirectory;
        IsDoor = isDoor;
        Children = new ObservableCollection<NodeViewModel>();
    }

    public string Name { get; }

    public string FullPath { get; }

    public bool IsDirectory { get; }

    /// <summary>A folder shown at the depth cap; clicking it re-roots the panel into it.</summary>
    public bool IsDoor { get; }

    public bool IsMarkdown =>
        !IsDirectory && Path.GetExtension(FullPath).Equals(".md", StringComparison.OrdinalIgnoreCase);

    /// <summary>True for a note Notty can edit (.md/.txt). Other files may be shown but not opened.</summary>
    public bool IsSupported =>
        IsDirectory || WorkspaceScanner.IsSupportedFile(FullPath);

    /// <summary>Leading icon, resolved to an embedded SVG by node type.</summary>
    public Uri IconSource =>
        IsDirectory ? FolderIcon
        : !IsSupported ? UnknownIcon
        : IsMarkdown ? MarkdownIcon
        : TextIcon;

    public ObservableCollection<NodeViewModel> Children { get; }

    public bool IsExpanded
    {
        get => _isExpanded;
        set => SetProperty(ref _isExpanded, value);
    }

    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }
}
