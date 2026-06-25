using System;
using System.Windows.Media;
using ICSharpCode.AvalonEdit.CodeCompletion;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Editing;

namespace Notty.App.Editor;

/// <summary>Adapts a <see cref="SlashCommand"/> to AvalonEdit's completion popup.</summary>
public sealed class SlashCommandCompletionData : ICompletionData
{
    private readonly SlashCommand _command;

    public SlashCommandCompletionData(SlashCommand command) => _command = command;

    public ImageSource Image => null!;
    public string Text => _command.Name;             // what the popup filters on
    public object Content => _command.Title;          // what the popup shows
    public object Description => _command.Description; // tooltip beside the selection
    public double Priority => 0;

    public void Complete(TextArea textArea, ISegment completionSegment, EventArgs insertionRequestEventArgs)
    {
        // The popup opened right after the '/', so the filter segment covers only what was typed
        // after it. Extend one char left to also swallow the '/' trigger.
        var start = completionSegment.Offset;
        var length = completionSegment.Length;
        if (start > 0 && textArea.Document.GetCharAt(start - 1) == '/')
        {
            start--;
            length++;
        }

        var (text, caret) = SlashCommands.Render(_command);
        textArea.Document.Replace(start, length, text);
        textArea.Caret.Offset = start + caret;
    }
}
