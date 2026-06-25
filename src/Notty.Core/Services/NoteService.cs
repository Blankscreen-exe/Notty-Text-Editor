using System.Text;

namespace Notty.Core.Services;

/// <summary>
/// Reads and writes note file contents. Plain UTF-8, no proprietary wrapping —
/// the file on disk is exactly what the user sees.
/// </summary>
public sealed class NoteService
{
    private static readonly UTF8Encoding Utf8NoBom = new(encoderShouldEmitUTF8Identifier: false);

    public string ReadText(string path) => File.ReadAllText(path);

    public void WriteText(string path, string content) =>
        File.WriteAllText(path, content, Utf8NoBom);
}
