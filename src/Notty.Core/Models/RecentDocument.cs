namespace Notty.Core.Models;

/// <summary>
/// A pointer to a previously opened document, persisted in settings.
/// </summary>
public sealed class RecentDocument
{
    public string FilePath { get; set; } = string.Empty;

    public DateTimeOffset LastOpened { get; set; }
}
