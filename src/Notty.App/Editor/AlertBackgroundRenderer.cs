using System.Windows;
using System.Windows.Media;
using ICSharpCode.AvalonEdit.Rendering;

namespace Notty.App.Editor;

/// <summary>
/// Paints the box behind GitHub alert blocks: a translucent full-width tint plus a solid left
/// accent bar, coloured per <see cref="AlertKind"/>. A <see cref="DocumentColorizingTransformer"/>
/// can only restyle text runs, so the box has to be drawn here on the background layer.
/// </summary>
public sealed class AlertBackgroundRenderer : IBackgroundRenderer
{
    public KnownLayer Layer => KnownLayer.Background;

    public void Draw(TextView textView, DrawingContext drawingContext)
    {
        if (textView.Document is null || !textView.VisualLinesValid)
            return;

        var width = textView.ActualWidth;

        foreach (var visualLine in textView.VisualLines)
        {
            var block = MarkdownAlerts.BlockFor(textView.Document, visualLine.FirstDocumentLine);
            if (block is null)
                continue;

            var (accent, tint) = MarkdownAlerts.Brushes(block.Value.Kind);
            var top = visualLine.VisualTop - textView.VerticalOffset;
            var height = visualLine.Height;

            drawingContext.DrawRectangle(tint, null, new Rect(0, top, width, height));
            drawingContext.DrawRectangle(accent, null, new Rect(0, top, 3, height));
        }
    }
}
