using System;
using System.Text.RegularExpressions;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Document;

namespace Notty.App.Editor;

/// <summary>
/// Smart keyboard editing for markdown lists. Enter on a list item with content starts a fresh
/// item; Enter on an empty item outdents one level or exits the list. Tab / Shift+Tab indent and
/// outdent the current item.
/// </summary>
public static class ListContinuation
{
    // Order matters: a task item is also an unordered item, so test it first.
    private static readonly Regex Task = new(@"^(\s*)([-*+])[ \t]+\[[ xX]\][ \t]?", RegexOptions.Compiled);
    private static readonly Regex Ordered = new(@"^(\s*)(\d+)([.)])[ \t]+", RegexOptions.Compiled);
    private static readonly Regex Unordered = new(@"^(\s*)([-*+])[ \t]+", RegexOptions.Compiled);

    /// <summary>
    /// Handles an Enter press. Returns true if it acted (the caller should mark the event handled),
    /// false to let the editor insert a normal newline.
    /// </summary>
    public static bool HandleEnter(TextEditor editor)
    {
        if (editor.SelectionLength > 0)
            return false; // Enter replacing a selection: leave it to the editor.

        var doc = editor.Document;
        var caret = editor.CaretOffset;
        var line = doc.GetLineByOffset(caret);
        var text = doc.GetText(line);

        if (!TryParse(text, out var indent, out var markerNoIndent, out var newMarker, out var prefixLength))
            return false;

        var isEmpty = text.Substring(prefixLength).Trim().Length == 0;

        if (isEmpty)
        {
            if (indent.Length > 0)
            {
                // Outdent one level, keeping the (still empty) marker.
                var replacement = RemoveOneIndentLevel(indent, editor) + markerNoIndent;
                doc.Replace(line.Offset, line.Length, replacement);
                editor.CaretOffset = line.Offset + replacement.Length;
            }
            else
            {
                // Already at the margin: drop the marker and leave a blank line.
                doc.Replace(line.Offset, line.Length, string.Empty);
                editor.CaretOffset = line.Offset;
            }

            return true;
        }

        // Continue the list: newline + same indent + a fresh marker. Any text after the caret moves
        // down and follows the new marker.
        var newline = line.DelimiterLength > 0
            ? doc.GetText(line.Offset + line.Length, line.DelimiterLength)
            : Environment.NewLine;
        var insert = newline + indent + newMarker;
        doc.Insert(caret, insert);
        editor.CaretOffset = caret + insert.Length;
        return true;
    }

    /// <summary>
    /// Handles a Tab (indent) or Shift+Tab (outdent) on a list line. Returns true if it acted.
    /// Selections are left to AvalonEdit's built-in block indent.
    /// </summary>
    public static bool HandleTab(TextEditor editor, bool outdent)
    {
        if (editor.SelectionLength > 0)
            return false;

        var doc = editor.Document;
        var caret = editor.CaretOffset;
        var line = doc.GetLineByOffset(caret);
        var text = doc.GetText(line);

        if (!IsListLine(text))
            return false;

        if (outdent)
        {
            var removed = LeadingIndentWidth(text, editor);
            if (removed > 0)
            {
                doc.Remove(line.Offset, removed);
                editor.CaretOffset = Math.Max(line.Offset, caret - removed);
            }

            return true; // own Tab on list lines even when there's nothing left to remove
        }

        var unit = IndentUnit(editor);
        doc.Insert(line.Offset, unit);
        editor.CaretOffset = caret + unit.Length;
        return true;
    }

    /// <summary>True when the line begins with a bullet, numbered, or task-list marker.</summary>
    public static bool IsListLine(string text) =>
        Task.IsMatch(text) || Ordered.IsMatch(text) || Unordered.IsMatch(text);

    private static string IndentUnit(TextEditor editor) =>
        editor.Options.ConvertTabsToSpaces
            ? new string(' ', Math.Max(1, editor.Options.IndentationSize))
            : "\t";

    /// <summary>Number of leading chars making up one indent step (a tab, or up to IndentationSize spaces).</summary>
    private static int LeadingIndentWidth(string text, TextEditor editor)
    {
        if (text.StartsWith("\t", StringComparison.Ordinal))
            return 1;

        var size = Math.Max(1, editor.Options.IndentationSize);
        var spaces = 0;
        while (spaces < size && spaces < text.Length && text[spaces] == ' ')
            spaces++;

        return spaces;
    }

    /// <summary>
    /// Parses the list marker on a line. <paramref name="markerNoIndent"/> is the existing marker
    /// (for outdenting), <paramref name="newMarker"/> is the marker for the next item.
    /// </summary>
    private static bool TryParse(string text, out string indent, out string markerNoIndent,
        out string newMarker, out int prefixLength)
    {
        if (Task.Match(text) is { Success: true } task)
        {
            indent = task.Groups[1].Value;
            markerNoIndent = task.Value.Substring(indent.Length);
            newMarker = $"{task.Groups[2].Value} [ ] "; // new task items always start unchecked
            prefixLength = task.Length;
            return true;
        }

        if (Ordered.Match(text) is { Success: true } ol)
        {
            indent = ol.Groups[1].Value;
            markerNoIndent = ol.Value.Substring(indent.Length);
            var next = int.TryParse(ol.Groups[2].Value, out var n) ? n + 1 : 1;
            newMarker = $"{next}{ol.Groups[3].Value} ";
            prefixLength = ol.Length;
            return true;
        }

        if (Unordered.Match(text) is { Success: true } ul)
        {
            indent = ul.Groups[1].Value;
            markerNoIndent = ul.Value.Substring(indent.Length);
            newMarker = $"{ul.Groups[2].Value} ";
            prefixLength = ul.Length;
            return true;
        }

        indent = markerNoIndent = newMarker = string.Empty;
        prefixLength = 0;
        return false;
    }

    /// <summary>Strips one indentation step (a tab, or up to IndentationSize spaces) from the end.</summary>
    private static string RemoveOneIndentLevel(string indent, TextEditor editor)
    {
        if (indent.EndsWith("\t", StringComparison.Ordinal))
            return indent.Substring(0, indent.Length - 1);

        var size = Math.Max(1, editor.Options.IndentationSize);
        var spaces = 0;
        while (spaces < size && indent.Length - 1 - spaces >= 0 && indent[indent.Length - 1 - spaces] == ' ')
            spaces++;

        return indent.Substring(0, indent.Length - spaces);
    }
}
