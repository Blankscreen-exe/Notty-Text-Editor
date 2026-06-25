using System;
using System.Collections.Generic;
using System.IO;

namespace Notty.App.Editor;

/// <summary>
/// One "/" command: a keyword to filter on, a title/description shown in the popup, and the snippet
/// it inserts. The snippet may contain <see cref="CaretMarker"/> to mark where the caret lands.
/// </summary>
public sealed record SlashCommand(string Name, string Title, string Description, string Template);

/// <summary>The catalog of slash commands, split by file type (markdown vs plain text).</summary>
public static class SlashCommands
{
    /// <summary>Placeholder inside a template marking where the caret should end up after insertion.</summary>
    public const string CaretMarker = "%CARET%";

    /// <summary>Returns the command set appropriate for the given file (.md vs everything else).</summary>
    public static IReadOnlyList<SlashCommand> For(string? path)
    {
        var ext = path is null ? string.Empty : Path.GetExtension(path).ToLowerInvariant();
        return ext == ".md" ? Markdown : Text;
    }

    /// <summary>Splits a template into the text to insert and the caret offset within it.</summary>
    public static (string Text, int CaretOffset) Render(SlashCommand command)
    {
        var idx = command.Template.IndexOf(CaretMarker, StringComparison.Ordinal);
        if (idx < 0)
            return (command.Template, command.Template.Length);

        return (command.Template.Remove(idx, CaretMarker.Length), idx);
    }

    public static readonly IReadOnlyList<SlashCommand> Markdown = new[]
    {
        new SlashCommand("heading", "Heading", "Section heading", "# %CARET%"),
        new SlashCommand("table", "Table", "Starter markdown table",
            "| Column | Column |\n| --- | --- |\n| %CARET% |  |\n|  |  |"),
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
}
