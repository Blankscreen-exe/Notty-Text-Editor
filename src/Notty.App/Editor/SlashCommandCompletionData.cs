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
    private readonly ISlashPrompts _prompts;

    public SlashCommandCompletionData(SlashCommand command, ISlashPrompts prompts)
    {
        _command = command;
        _prompts = prompts;
    }

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

        // Remove the "/command" text first so an interactive prompt isn't shown over stale text.
        textArea.Document.Replace(start, length, string.Empty);

        var insertion = _command.Interactive is not null
            ? _command.Interactive(_prompts)
            : SlashCommands.Render(_command);

        if (insertion is null) // cancelled prompt: leave the trigger removed, caret in place
        {
            textArea.Caret.Offset = start;
            return;
        }

        var (text, caret) = insertion.Value;
        textArea.Document.Insert(start, text);
        textArea.Caret.Offset = start + caret;
    }
}
