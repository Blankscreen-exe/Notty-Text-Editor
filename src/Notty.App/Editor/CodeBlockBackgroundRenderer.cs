using System.Windows;
using System.Windows.Media;
using ICSharpCode.AvalonEdit.Rendering;

namespace Notty.App.Editor;

/// <summary>
/// Paints the full-width background behind fenced code blocks. The colorizer can only tint the area
/// behind the glyphs themselves, so the block fill (including blank space and the fence lines) is
/// drawn here on the background layer.
/// </summary>
public sealed class CodeBlockBackgroundRenderer : IBackgroundRenderer
{
    public KnownLayer Layer => KnownLayer.Background;

    public void Draw(TextView textView, DrawingContext drawingContext)
    {
        if (textView.Document is null || !textView.VisualLinesValid)
            return;

        if (Application.Current.Resources["CodeBackgroundBrush"] is not Brush background)
            return;

        var width = textView.ActualWidth;

        foreach (var visualLine in textView.VisualLines)
        {
            if (!MarkdownFences.IsInCodeBlock(textView.Document, visualLine.FirstDocumentLine))
                continue;

            var top = visualLine.VisualTop - textView.VerticalOffset;
            drawingContext.DrawRectangle(background, null, new Rect(0, top, width, visualLine.Height));
        }
    }
}
