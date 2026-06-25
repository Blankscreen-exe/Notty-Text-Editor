namespace Notty.App.ViewModels;

/// <summary>One segment of the path breadcrumb above the folder tree.</summary>
public sealed class BreadcrumbItem
{
    public BreadcrumbItem(string name, string fullPath, bool isCurrent)
    {
        Name = name;
        FullPath = fullPath;
        IsCurrent = isCurrent;
    }

    public string Name { get; }

    public string FullPath { get; }

    /// <summary>True for the last segment (the current root) — rendered non-interactive/bold.</summary>
    public bool IsCurrent { get; }
}
