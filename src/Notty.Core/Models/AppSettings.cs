namespace Notty.Core.Models;

/// <summary>
/// User settings, persisted as JSON in %AppData%\Notty\settings.json.
/// Kept deliberately small and separate from the note files themselves.
/// </summary>
public sealed class AppSettings
{
    /// <summary>Absolute path to the active workspace folder, or null on first launch.</summary>
    public string? WorkspacePath { get; set; }

    /// <summary>Selected theme name (matches a palette's display name, e.g. "Light", "Dark").</summary>
    public string Theme { get; set; } = "Light";

    /// <summary>When true, files of unsupported types are still shown in the tree (with a "?" icon).</summary>
    public bool ShowUnsupportedFiles { get; set; }

    // ---- Editor display settings ----
    public string EditorFontFamily { get; set; } = "Consolas";
    public double EditorFontSize { get; set; } = 14;
    public bool EditorWordWrap { get; set; } = true;
    public bool EditorShowLineNumbers { get; set; } = true;
    public int EditorTabWidth { get; set; } = 4;
    public bool EditorHighlightCurrentLine { get; set; }

    /// <summary>When true, markdown files render inline formatting in the editor (still editable).</summary>
    public bool MarkdownPreview { get; set; }

    /// <summary>When true, edits are written automatically shortly after typing stops. When false, save is manual (Ctrl+S).</summary>
    public bool AutoSave { get; set; } = true;

    /// <summary>Most-recently-opened documents, newest first. Capped to <see cref="MaxRecentDocuments"/>.</summary>
    public List<RecentDocument> RecentDocuments { get; set; } = new();

    public const int MaxRecentDocuments = 20;
}
