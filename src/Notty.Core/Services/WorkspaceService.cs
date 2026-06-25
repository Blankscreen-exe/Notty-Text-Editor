using System.Text;

namespace Notty.Core.Services;

/// <summary>
/// Structural mutations on the workspace: creating notes and categories (folders).
/// Rename / delete / move will join these as the file-management features grow.
/// All names are validated and collisions are resolved by appending " (n)".
/// </summary>
public sealed class WorkspaceService
{
    private static readonly UTF8Encoding Utf8NoBom = new(encoderShouldEmitUTF8Identifier: false);

    /// <summary>
    /// Creates an empty note in <paramref name="directory"/> with the given base name and
    /// extension (".md" or ".txt"). Returns the full path actually created.
    /// </summary>
    public string CreateNote(string directory, string baseName, string extension)
    {
        ValidateName(baseName);
        if (!extension.StartsWith('.'))
            extension = "." + extension;

        var path = MakeUniquePath(Path.Combine(directory, baseName + extension));
        File.WriteAllText(path, string.Empty, Utf8NoBom);
        return path;
    }

    /// <summary>Creates a sub-folder (category) under <paramref name="parentDirectory"/>. Returns its full path.</summary>
    public string CreateCategory(string parentDirectory, string name)
    {
        ValidateName(name);
        var path = MakeUniquePath(Path.Combine(parentDirectory, name));
        Directory.CreateDirectory(path);
        return path;
    }

    /// <summary>
    /// Renames a note or category to <paramref name="newLeafName"/> (the full leaf name, including
    /// extension for files). Returns the new full path. Throws if the target name already exists.
    /// </summary>
    public string Rename(string path, string newLeafName)
    {
        ValidateName(newLeafName);

        var dir = Path.GetDirectoryName(path)
            ?? throw new ArgumentException("Cannot rename a root path.", nameof(path));
        var target = Path.Combine(dir, newLeafName);

        if (PathsEqual(target, path))
            return path; // unchanged

        if (File.Exists(target) || Directory.Exists(target))
            throw new IOException($"An item named \"{newLeafName}\" already exists here.");

        if (Directory.Exists(path))
            Directory.Move(path, target);
        else
            File.Move(path, target);

        return target;
    }

    /// <summary>Copies a note alongside the original as "name (copy).ext" (uniquified). Returns the new path.</summary>
    public string Duplicate(string filePath)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException("File not found.", filePath);

        var dir = Path.GetDirectoryName(filePath)!;
        var name = Path.GetFileNameWithoutExtension(filePath);
        var ext = Path.GetExtension(filePath);

        var target = MakeUniquePath(Path.Combine(dir, $"{name} (copy){ext}"));
        File.Copy(filePath, target);
        return target;
    }

    private static bool PathsEqual(string a, string b) =>
        string.Equals(Path.GetFullPath(a), Path.GetFullPath(b), StringComparison.OrdinalIgnoreCase);

    /// <summary>Throws <see cref="ArgumentException"/> with a user-friendly message for empty or invalid names.</summary>
    public static void ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name cannot be empty.");

        if (name.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            throw new ArgumentException("Name contains invalid characters such as \\ / : * ? \" < > |.");
    }

    /// <summary>
    /// If <paramref name="desiredPath"/> already exists, appends " (2)", " (3)", … to the
    /// name (before any extension) until the path is free.
    /// </summary>
    public static string MakeUniquePath(string desiredPath)
    {
        if (!Exists(desiredPath))
            return desiredPath;

        var dir = Path.GetDirectoryName(desiredPath)!;
        var name = Path.GetFileNameWithoutExtension(desiredPath);
        var ext = Path.GetExtension(desiredPath);

        for (var i = 2; ; i++)
        {
            var candidate = Path.Combine(dir, $"{name} ({i}){ext}");
            if (!Exists(candidate))
                return candidate;
        }
    }

    private static bool Exists(string path) => File.Exists(path) || Directory.Exists(path);
}
