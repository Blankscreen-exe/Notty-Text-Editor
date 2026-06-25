using System.Diagnostics;
using System.IO;
using Microsoft.VisualBasic.FileIO;

namespace Notty.App.Services;

/// <summary>
/// Windows shell integrations that don't belong in the portable Core layer:
/// sending items to the Recycle Bin and revealing them in File Explorer.
/// </summary>
public static class ShellOperations
{
    /// <summary>Sends a file or folder to the Recycle Bin (with the standard shell confirmation/undo).</summary>
    public static void SendToRecycleBin(string path)
    {
        if (Directory.Exists(path))
            FileSystem.DeleteDirectory(path, UIOption.OnlyErrorDialogs, RecycleOption.SendToRecycleBin);
        else if (File.Exists(path))
            FileSystem.DeleteFile(path, UIOption.OnlyErrorDialogs, RecycleOption.SendToRecycleBin);
    }

    /// <summary>Opens File Explorer with the item selected (or the folder opened).</summary>
    public static void RevealInExplorer(string path)
    {
        if (Directory.Exists(path))
            Process.Start(new ProcessStartInfo("explorer.exe", $"\"{path}\"") { UseShellExecute = true });
        else if (File.Exists(path))
            Process.Start(new ProcessStartInfo("explorer.exe", $"/select,\"{path}\"") { UseShellExecute = true });
    }
}
