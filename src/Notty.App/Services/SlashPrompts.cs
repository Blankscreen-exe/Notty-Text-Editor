using System;
using System.IO;
using Notty.App.Editor;

namespace Notty.App.Services;

/// <summary>
/// Implements the slash-command prompts over <see cref="DialogService"/>. Knows the current note's
/// path so it can store image references relative to the note when possible.
/// </summary>
public sealed class SlashPrompts : ISlashPrompts
{
    private readonly DialogService _dialogs;
    private readonly string? _currentFilePath;

    public SlashPrompts(DialogService dialogs, string? currentFilePath)
    {
        _dialogs = dialogs;
        _currentFilePath = currentFilePath;
    }

    public (int Rows, int Cols)? AskTableSize()
    {
        var input = _dialogs.PromptForName("Insert Table", "Size as rows x columns (e.g. 3x4):", "2x3");
        if (string.IsNullOrWhiteSpace(input))
            return null;

        var parts = input.Split(new[] { 'x', 'X', '×', ',' },
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (parts.Length == 2 &&
            int.TryParse(parts[0], out var rows) && rows > 0 &&
            int.TryParse(parts[1], out var cols) && cols > 0)
            return (rows, cols);

        _dialogs.ShowError($"Couldn't read \"{input}\" as a table size. Use a format like 3x4.");
        return null;
    }

    public string? AskImagePath()
    {
        var path = _dialogs.PickFile("Choose Image",
            "Images|*.png;*.jpg;*.jpeg;*.gif;*.bmp;*.webp;*.svg|All files|*.*");
        return path is null ? null : ToMarkdownPath(path);
    }

    /// <summary>
    /// Makes the path relative to the note's folder when possible, switches to forward slashes, and
    /// wraps it in angle brackets if it contains spaces (so the markdown link stays valid).
    /// </summary>
    private string ToMarkdownPath(string absolutePath)
    {
        var path = absolutePath;
        var noteDir = _currentFilePath is null ? null : Path.GetDirectoryName(_currentFilePath);
        if (noteDir is not null)
        {
            try
            {
                var relative = Path.GetRelativePath(noteDir, absolutePath);
                if (!Path.IsPathRooted(relative))
                    path = relative;
            }
            catch
            {
                // Different volume, etc. — keep the absolute path.
            }
        }

        path = path.Replace('\\', '/');
        return path.Contains(' ') ? $"<{path}>" : path;
    }
}
