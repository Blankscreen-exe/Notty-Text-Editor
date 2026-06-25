using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Windows.Media;
using ICSharpCode.AvalonEdit.Document;

namespace Notty.App.Editor;

/// <summary>The five GitHub "alert" callout kinds.</summary>
public enum AlertKind { Note, Tip, Important, Warning, Caution }

/// <summary>What an alert detector knows about a single line inside an alert block.</summary>
public readonly record struct AlertBlock(AlertKind Kind, bool IsHeader, bool IsFirst, bool IsLast);

/// <summary>
/// Detects GitHub-style alerts — a blockquote whose first line is <c>&gt; [!NOTE]</c> (or TIP /
/// IMPORTANT / WARNING / CAUTION) — and supplies their colours. Shared by the colorizer (which
/// styles the marker + label text) and <see cref="AlertBackgroundRenderer"/> (which paints the box).
/// </summary>
public static class MarkdownAlerts
{
    // GitHub strictly wants the [!TYPE] marker alone on the first line, but we also accept content on
    // the same line (> [!NOTE] text) since that is what people naturally type.
    private static readonly Regex HeaderPattern =
        new(@"^\s{0,3}>\s?\[!(NOTE|TIP|IMPORTANT|WARNING|CAUTION)\]",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Dictionary<AlertKind, (SolidColorBrush Accent, SolidColorBrush Tint)> BrushCache = new();

    public static bool IsQuoteLine(string text) =>
        text.TrimStart().StartsWith(">", System.StringComparison.Ordinal);

    private static bool TryHeaderKind(string text, out AlertKind kind)
    {
        var m = HeaderPattern.Match(text);
        if (m.Success)
        {
            kind = m.Groups[1].Value.ToUpperInvariant() switch
            {
                "NOTE" => AlertKind.Note,
                "TIP" => AlertKind.Tip,
                "IMPORTANT" => AlertKind.Important,
                "WARNING" => AlertKind.Warning,
                _ => AlertKind.Caution,
            };
            return true;
        }

        kind = default;
        return false;
    }

    /// <summary>
    /// Returns the alert block this line belongs to, or null when the line is not part of a
    /// blockquote whose first line is an alert header.
    /// </summary>
    public static AlertBlock? BlockFor(IDocument doc, DocumentLine line)
    {
        if (!IsQuoteLine(doc.GetText(line)))
            return null;

        // Climb to the first line of this contiguous blockquote.
        var top = line;
        while (top.PreviousLine is { } prev && IsQuoteLine(doc.GetText(prev)))
            top = prev;

        if (!TryHeaderKind(doc.GetText(top), out var kind))
            return null;

        // Descend to the last line of the blockquote.
        var bottom = line;
        while (bottom.NextLine is { } next && IsQuoteLine(doc.GetText(next)))
            bottom = next;

        return new AlertBlock(kind,
            IsHeader: line.Offset == top.Offset,
            IsFirst: line.Offset == top.Offset,
            IsLast: line.Offset == bottom.Offset);
    }

    /// <summary>Friendly title GitHub shows next to the icon ("Note", "Warning", …).</summary>
    public static string Label(AlertKind kind) => kind switch
    {
        AlertKind.Note => "Note",
        AlertKind.Tip => "Tip",
        AlertKind.Important => "Important",
        AlertKind.Warning => "Warning",
        _ => "Caution",
    };

    /// <summary>Frozen, cached brushes: solid accent (bar + label) and translucent tint (box fill).</summary>
    public static (SolidColorBrush Accent, SolidColorBrush Tint) Brushes(AlertKind kind)
    {
        if (BrushCache.TryGetValue(kind, out var pair))
            return pair;

        var accent = kind switch
        {
            AlertKind.Note => Color.FromRgb(0x2F, 0x81, 0xF7),       // blue
            AlertKind.Tip => Color.FromRgb(0x3F, 0xB9, 0x50),        // green
            AlertKind.Important => Color.FromRgb(0xA3, 0x71, 0xF7),  // purple
            AlertKind.Warning => Color.FromRgb(0xD2, 0x99, 0x22),    // amber
            _ => Color.FromRgb(0xF8, 0x51, 0x49),                    // red (caution)
        };

        // Translucent fill works over both light and dark editor backgrounds.
        var tint = Color.FromArgb(0x26, accent.R, accent.G, accent.B);

        var accentBrush = new SolidColorBrush(accent);
        var tintBrush = new SolidColorBrush(tint);
        accentBrush.Freeze();
        tintBrush.Freeze();

        pair = (accentBrush, tintBrush);
        BrushCache[kind] = pair;
        return pair;
    }
}
