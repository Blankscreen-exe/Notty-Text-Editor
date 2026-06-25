using System;
using ICSharpCode.AvalonEdit.Document;

namespace Notty.App.Editor;

/// <summary>
/// Shared detection for fenced code blocks (``` … ```), used by both the colorizer (text styling)
/// and <see cref="CodeBlockBackgroundRenderer"/> (the full-width block fill).
/// </summary>
public static class MarkdownFences
{
    public static bool IsFenceLine(string text) =>
        text.TrimStart().StartsWith("```", StringComparison.Ordinal);

    /// <summary>
    /// True when the line is part of a fenced code block — either a ``` fence line itself or a line
    /// sitting between an opening and closing fence.
    /// </summary>
    public static bool IsInCodeBlock(IDocument doc, DocumentLine line)
    {
        var fencesBefore = 0;
        for (var n = 1; n < line.LineNumber; n++)
        {
            if (IsFenceLine(doc.GetText(doc.GetLineByNumber(n))))
                fencesBefore++;
        }

        // Odd number of fences before => inside a block. Fence lines themselves are always part of it.
        return fencesBefore % 2 == 1 || IsFenceLine(doc.GetText(line));
    }
}
