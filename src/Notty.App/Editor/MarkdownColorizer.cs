using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Media;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Rendering;

namespace Notty.App.Editor;

/// <summary>
/// Styles markdown inline inside AvalonEdit while leaving the text fully editable. Visual only —
/// it never changes the document. Handles headings, bold/italic, inline + fenced code, bulleted /
/// numbered / task lists, blockquotes and tables.
/// </summary>
public sealed class MarkdownColorizer : DocumentColorizingTransformer
{
    private static readonly Regex Heading = new(@"^(#{1,6})\s+\S", RegexOptions.Compiled);
    private static readonly Regex HorizontalRule = new(@"^\s{0,3}([-*_])(?:\s*\1){2,}\s*$", RegexOptions.Compiled);
    private static readonly Regex Code = new(@"`([^`\n]+)`", RegexOptions.Compiled);
    private static readonly Regex BoldItalic = new(@"(\*\*\*|___)(?=\S)(.+?)(?<=\S)\1", RegexOptions.Compiled);
    private static readonly Regex Bold = new(@"(\*\*|__)(?=\S)(.+?)(?<=\S)\1", RegexOptions.Compiled);
    private static readonly Regex Italic =
        new(@"(?<![\*_\w])([\*_])(?![\*_\s])(.+?)(?<![\*_\s])\1(?![\*_\w])", RegexOptions.Compiled);
    private static readonly Regex Strikethrough = new(@"~~(?=\S)(.+?)(?<=\S)~~", RegexOptions.Compiled);
    private static readonly Regex Image = new(@"!\[([^\]]*)\]\(([^)\s]+)[^)]*\)", RegexOptions.Compiled);
    private static readonly Regex Link = new(@"(?<!\!)\[([^\]]+)\]\(([^)\s]+)[^)]*\)", RegexOptions.Compiled);
    private static readonly Regex AutoLink = new(@"<((?:https?|ftp)://[^>\s]+)>", RegexOptions.Compiled);
    private static readonly Regex TaskItem = new(@"^(\s*[-*+]\s+)\[([ xX])\](\s)", RegexOptions.Compiled);
    private static readonly Regex Unordered = new(@"^(\s*)([-*+])(\s+)", RegexOptions.Compiled);
    private static readonly Regex Ordered = new(@"^(\s*)(\d+[.)])(\s+)", RegexOptions.Compiled);

    private readonly TextEditor _editor;

    public MarkdownColorizer(TextEditor editor) => _editor = editor;

    protected override void ColorizeLine(DocumentLine line)
    {
        if (line.Length == 0)
            return;

        var text = CurrentContext.Document.GetText(line);
        var start = line.Offset;

        var muted = Brush("MutedTextBrush", Brushes.Gray);
        var accent = Brush("AccentBrush", Brushes.SteelBlue);

        // Fenced code blocks (``` … ```), spanning multiple lines.
        if (text.TrimStart().StartsWith("```", StringComparison.Ordinal) || IsInsideFence(line))
        {
            var codeBg = Brush("CodeBackgroundBrush", null);
            var codeText = Brush("CodeTextBrush", muted);
            ChangeLinePart(start, line.EndOffset, e =>
            {
                ApplyMonospace(e);
                e.TextRunProperties.SetForegroundBrush(codeText);
                if (codeBg is not null)
                    e.TextRunProperties.SetBackgroundBrush(codeBg);
            });
            return;
        }

        // GitHub alerts (> [!NOTE] … blocks): fade the quote marker and colour the [!TYPE] label;
        // the tinted box + accent bar are painted by AlertBackgroundRenderer.
        if (MarkdownAlerts.BlockFor(CurrentContext.Document, line) is { } alert)
        {
            var quote = text.IndexOf('>');
            if (quote >= 0)
                Dim(start + quote, 1, muted);

            if (alert.IsHeader)
            {
                var label = MarkdownAlerts.Brushes(alert.Kind).Accent;
                var bracket = text.IndexOf('[');
                if (bracket >= 0)
                    ChangeLinePart(start + bracket, line.EndOffset, e =>
                    {
                        ApplyWeight(e, FontWeights.Bold);
                        e.TextRunProperties.SetForegroundBrush(label);
                    });
            }
            else
            {
                ApplyInline(text, start, muted);
            }
            return;
        }

        // Headings: enlarge + bold the whole line, dim the leading hashes.
        var heading = Heading.Match(text);
        if (heading.Success)
        {
            var size = _editor.FontSize * HeadingScale(heading.Groups[1].Length);
            ChangeLinePart(start, line.EndOffset, e =>
            {
                e.TextRunProperties.SetFontRenderingEmSize(size);
                ApplyWeight(e, FontWeights.Bold);
            });
            Dim(start, heading.Groups[1].Length + 1, muted);
            return;
        }

        // Blockquote: italicise + dim the whole line.
        if (text.StartsWith("> ", StringComparison.Ordinal))
        {
            ChangeLinePart(start, line.EndOffset, e =>
            {
                ApplyStyle(e, FontStyles.Italic);
                e.TextRunProperties.SetForegroundBrush(muted);
            });
            return;
        }

        // Horizontal rule (---, ***, ___): dim the whole line so it reads as a divider.
        if (HorizontalRule.IsMatch(text))
        {
            ChangeLinePart(start, line.EndOffset, e => e.TextRunProperties.SetForegroundBrush(muted));
            return;
        }

        // Tables: dim the pipes; bold the header row (the one above a |---|---| separator).
        if (LooksLikeTableRow(text))
        {
            if (IsTableSeparator(text))
            {
                ChangeLinePart(start, line.EndOffset, e => e.TextRunProperties.SetForegroundBrush(muted));
                return;
            }

            if (NextLineIsSeparator(line))
                ChangeLinePart(start, line.EndOffset, e => ApplyWeight(e, FontWeights.Bold));

            for (var i = 0; i < text.Length; i++)
                if (text[i] == '|')
                    Dim(start + i, 1, muted);

            ApplyInline(text, start, muted);
            return;
        }

        // List markers (task / bulleted / numbered): colour the marker with the accent.
        var task = TaskItem.Match(text);
        if (task.Success)
        {
            var markerEnd = task.Groups[3].Index; // through the closing ']'
            ChangeLinePart(start, start + markerEnd, e => { ApplyWeight(e, FontWeights.Bold); e.TextRunProperties.SetForegroundBrush(accent); });

            var isChecked = task.Groups[2].Value is "x" or "X";
            if (isChecked)
                ChangeLinePart(start + markerEnd, line.EndOffset, e =>
                {
                    e.TextRunProperties.SetForegroundBrush(muted);
                    e.TextRunProperties.SetTextDecorations(TextDecorations.Strikethrough);
                });
        }
        else if (Unordered.Match(text) is { Success: true } ul)
        {
            ChangeLinePart(start + ul.Groups[2].Index, start + ul.Groups[2].Index + 1,
                e => { ApplyWeight(e, FontWeights.Bold); e.TextRunProperties.SetForegroundBrush(accent); });
        }
        else if (Ordered.Match(text) is { Success: true } ol)
        {
            ChangeLinePart(start + ol.Groups[2].Index, start + ol.Groups[2].Index + ol.Groups[2].Length,
                e => { ApplyWeight(e, FontWeights.Bold); e.TextRunProperties.SetForegroundBrush(accent); });
        }

        ApplyInline(text, start, muted);
    }

    // ---- inline spans (bold / italic / code) ----

    private void ApplyInline(string text, int start, Brush muted)
    {
        var codeBg = Brush("CodeBackgroundBrush", null);
        var codeText = Brush("CodeTextBrush", muted);
        var accent = Brush("AccentBrush", Brushes.SteelBlue);

        foreach (Match m in Code.Matches(text))
        {
            ChangeLinePart(start + m.Index, start + m.Index + m.Length, e =>
            {
                ApplyMonospace(e);
                e.TextRunProperties.SetForegroundBrush(codeText);
                if (codeBg is not null)
                    e.TextRunProperties.SetBackgroundBrush(codeBg);
            });
        }

        // Bold+italic (***…*** / ___…___) first, so the plainer bold/italic passes don't fight it.
        foreach (Match m in BoldItalic.Matches(text))
        {
            ChangeLinePart(start + m.Index, start + m.Index + m.Length, e =>
            {
                ApplyWeight(e, FontWeights.Bold);
                ApplyStyle(e, FontStyles.Italic);
            });
            Dim(start + m.Index, 3, muted);
            Dim(start + m.Index + m.Length - 3, 3, muted);
        }

        foreach (Match m in Bold.Matches(text))
        {
            ChangeLinePart(start + m.Index, start + m.Index + m.Length, e => ApplyWeight(e, FontWeights.Bold));
            Dim(start + m.Index, m.Groups[1].Length, muted);
            Dim(start + m.Index + m.Length - m.Groups[1].Length, m.Groups[1].Length, muted);
        }

        foreach (Match m in Italic.Matches(text))
        {
            ChangeLinePart(start + m.Index, start + m.Index + m.Length, e => ApplyStyle(e, FontStyles.Italic));
            Dim(start + m.Index, 1, muted);
            Dim(start + m.Index + m.Length - 1, 1, muted);
        }

        foreach (Match m in Strikethrough.Matches(text))
        {
            ChangeLinePart(start + m.Index, start + m.Index + m.Length,
                e => e.TextRunProperties.SetTextDecorations(TextDecorations.Strikethrough));
            Dim(start + m.Index, 2, muted);
            Dim(start + m.Index + m.Length - 2, 2, muted);
        }

        // Images ![alt](url): colour the alt text, dim the surrounding syntax + url.
        foreach (Match m in Image.Matches(text))
        {
            Dim(start + m.Index, m.Length, muted);
            var alt = m.Groups[1];
            ChangeLinePart(start + alt.Index, start + alt.Index + alt.Length, e =>
            {
                ApplyStyle(e, FontStyles.Italic);
                e.TextRunProperties.SetForegroundBrush(accent);
            });
        }

        // Links [text](url): accent + underline the visible text, dim the brackets + url.
        foreach (Match m in Link.Matches(text))
        {
            Dim(start + m.Index, m.Length, muted);
            var label = m.Groups[1];
            ChangeLinePart(start + label.Index, start + label.Index + label.Length, e =>
            {
                e.TextRunProperties.SetForegroundBrush(accent);
                e.TextRunProperties.SetTextDecorations(TextDecorations.Underline);
            });
        }

        // Autolinks <https://…>: accent + underline the url, dim the angle brackets.
        foreach (Match m in AutoLink.Matches(text))
        {
            ChangeLinePart(start + m.Index, start + m.Index + m.Length, e =>
            {
                e.TextRunProperties.SetForegroundBrush(accent);
                e.TextRunProperties.SetTextDecorations(TextDecorations.Underline);
            });
            Dim(start + m.Index, 1, muted);
            Dim(start + m.Index + m.Length - 1, 1, muted);
        }
    }

    // ---- helpers ----

    private bool IsInsideFence(DocumentLine line)
    {
        var doc = CurrentContext.Document;
        var fences = 0;
        for (var n = 1; n < line.LineNumber; n++)
        {
            var text = doc.GetText(doc.GetLineByNumber(n));
            if (text.TrimStart().StartsWith("```", StringComparison.Ordinal))
                fences++;
        }
        return fences % 2 == 1;
    }

    private static bool LooksLikeTableRow(string text) => text.Count(c => c == '|') >= 2;

    private static bool IsTableSeparator(string text)
    {
        var trimmed = text.Trim();
        return trimmed.Contains('-') && trimmed.All(c => c is '|' or '-' or ':' or ' ');
    }

    private bool NextLineIsSeparator(DocumentLine line)
    {
        var next = line.NextLine;
        if (next is null)
            return false;
        var text = CurrentContext.Document.GetText(next);
        return LooksLikeTableRow(text) && IsTableSeparator(text);
    }

    private void Dim(int offset, int length, Brush brush)
    {
        if (length > 0)
            ChangeLinePart(offset, offset + length, e => e.TextRunProperties.SetForegroundBrush(brush));
    }

    private static double HeadingScale(int level) => level switch
    {
        1 => 1.6,
        2 => 1.4,
        3 => 1.25,
        4 => 1.15,
        5 => 1.08,
        _ => 1.02,
    };

    private static void ApplyWeight(VisualLineElement e, FontWeight weight)
    {
        var tf = e.TextRunProperties.Typeface;
        e.TextRunProperties.SetTypeface(new Typeface(tf.FontFamily, tf.Style, weight, tf.Stretch));
    }

    private static void ApplyStyle(VisualLineElement e, FontStyle style)
    {
        var tf = e.TextRunProperties.Typeface;
        e.TextRunProperties.SetTypeface(new Typeface(tf.FontFamily, style, tf.Weight, tf.Stretch));
    }

    private static void ApplyMonospace(VisualLineElement e) =>
        e.TextRunProperties.SetTypeface(
            new Typeface(new FontFamily("Consolas"), FontStyles.Normal, FontWeights.Normal, FontStretches.Normal));

    private static Brush Brush(string resourceKey, Brush? fallback) =>
        Application.Current.Resources[resourceKey] as Brush ?? fallback!;
}
