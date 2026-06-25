using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Notty.App.Editor;

/// <summary>
/// Prompts an interactive slash command can use to gather input. Implemented in the app layer over
/// the dialog service, so this editor code stays free of WPF dialog dependencies.
/// </summary>
public interface ISlashPrompts
{
    /// <summary>Asks for a table size. Returns null if cancelled.</summary>
    (int Rows, int Cols)? AskTableSize();

    /// <summary>Asks for an image, returning a markdown-ready path. Returns null if cancelled.</summary>
    string? AskImagePath();
}

/// <summary>
/// One "/" command. Most insert a static <see cref="Template"/>; an interactive command instead
/// supplies <see cref="Interactive"/>, which prompts the user and builds the snippet (or returns
/// null to cancel). A template/built snippet may contain <see cref="SlashCommands.CaretMarker"/>.
/// </summary>
public sealed record SlashCommand(
    string Name,
    string Title,
    string Description,
    string? Template = null,
    Func<ISlashPrompts, (string Text, int CaretOffset)?>? Interactive = null);

/// <summary>The catalog of slash commands, split by file type (markdown vs plain text).</summary>
public static class SlashCommands
{
    /// <summary>Placeholder inside a snippet marking where the caret should end up after insertion.</summary>
    public const string CaretMarker = "%CARET%";

    /// <summary>Returns the command set appropriate for the given file (.md vs everything else).</summary>
    public static IReadOnlyList<SlashCommand> For(string? path)
    {
        var ext = path is null ? string.Empty : Path.GetExtension(path).ToLowerInvariant();
        return ext == ".md" ? Markdown : Text;
    }

    /// <summary>Splits a snippet into the text to insert and the caret offset within it.</summary>
    public static (string Text, int CaretOffset) SplitCaret(string snippet)
    {
        var idx = snippet.IndexOf(CaretMarker, StringComparison.Ordinal);
        return idx < 0 ? (snippet, snippet.Length) : (snippet.Remove(idx, CaretMarker.Length), idx);
    }

    /// <summary>Renders a static command's template.</summary>
    public static (string Text, int CaretOffset) Render(SlashCommand command) =>
        SplitCaret(command.Template ?? string.Empty);

    public static readonly IReadOnlyList<SlashCommand> Markdown = new[]
    {
        new SlashCommand("heading", "Heading", "Section heading", "# %CARET%"),
        new SlashCommand("table", "Table", "Markdown table (choose size)", Interactive: BuildTable),
        new SlashCommand("image", "Image", "Insert an image (choose file)", Interactive: BuildImage),
        new SlashCommand("ul", "Bulleted list", "Unordered list item", "- %CARET%"),
        new SlashCommand("ol", "Numbered list", "Ordered list item", "1. %CARET%"),
        new SlashCommand("checklist", "Checklist", "Task list item", "- [ ] %CARET%"),
        new SlashCommand("quote", "Quote", "Blockquote", "> %CARET%"),
        new SlashCommand("code", "Code block", "Fenced code block", "```\n%CARET%\n```"),
        new SlashCommand("note", "Info box", "Blue [!NOTE] callout", "> [!NOTE]\n> %CARET%"),
        new SlashCommand("tip", "Tip box", "Green [!TIP] callout", "> [!TIP]\n> %CARET%"),
        new SlashCommand("important", "Important box", "Purple [!IMPORTANT] callout", "> [!IMPORTANT]\n> %CARET%"),
        new SlashCommand("warning", "Warning box", "Amber [!WARNING] callout", "> [!WARNING]\n> %CARET%"),
        new SlashCommand("caution", "Caution box", "Red [!CAUTION] callout", "> [!CAUTION]\n> %CARET%"),
        new SlashCommand("divider", "Divider", "Horizontal rule", "---\n%CARET%"),
    };

    public static readonly IReadOnlyList<SlashCommand> Text = new[]
    {
        new SlashCommand("divider", "Divider line", "Dashed separator",
            "------------------------------\n%CARET%"),
        new SlashCommand("banner", "Banner", "Boxed title banner",
            "==============================\n  %CARET%\n=============================="),
        new SlashCommand("section", "Section header", "Title with an underline",
            "%CARET%\n------------------------------"),
        new SlashCommand("bullet", "Bullet", "Bullet point", "• %CARET%"),
        new SlashCommand("arrow", "Arrow", "Arrow bullet", "→ %CARET%"),
        new SlashCommand("check", "Check mark", "Check-marked line", "✔ %CARET%"),
        new SlashCommand("todo", "To-do", "Empty checkbox line", "[ ] %CARET%"),
        new SlashCommand("note", "Note label", "NOTE: prefix", "NOTE: %CARET%"),
    };

    // ---- interactive builders ----

    private static (string Text, int CaretOffset)? BuildTable(ISlashPrompts prompts)
    {
        if (prompts.AskTableSize() is not { } size)
            return null;

        var cols = Math.Clamp(size.Cols, 1, 20);
        var rows = Math.Clamp(size.Rows, 1, 50);

        // Empty cells, caret in the first header cell. Each non-first cell is "  |".
        var header = "| %CARET% |" + string.Concat(Enumerable.Repeat("  |", cols - 1));
        var separator = "|" + string.Concat(Enumerable.Repeat(" --- |", cols));
        var bodyRow = "|" + string.Concat(Enumerable.Repeat("  |", cols));

        var snippet = header + "\n" + separator + string.Concat(Enumerable.Repeat("\n" + bodyRow, rows));
        return SplitCaret(snippet);
    }

    private static (string Text, int CaretOffset)? BuildImage(ISlashPrompts prompts)
    {
        if (prompts.AskImagePath() is not { } path)
            return null;

        return SplitCaret($"![%CARET%]({path})");
    }
}
