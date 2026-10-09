namespace KevlarEDR.Core;

public sealed record FileEntry(string Name, string FullPath, bool IsDirectory, bool IsLink,
    long? Size, DateTime LastModified, FileAttributes Attributes);
public sealed record DirectorySnapshot(string Path, IReadOnlyList<FileEntry> Entries, bool Truncated, int Skipped);

/// <summary>Bounded, non-recursive local metadata browsing. Never opens file content.</summary>
public sealed class LocalFileBrowser(int maxEntries = 5000)
{
    public int MaxEntries { get; } = maxEntries > 0 ? maxEntries : throw new ArgumentOutOfRangeException(nameof(maxEntries));
    public static string ValidateLocation(string location)
    {
        if (!Path.IsPathFullyQualified(location) || location.StartsWith(@"\\", StringComparison.Ordinal))
            throw new ArgumentException("Enter a full local folder path, such as C:\\Users. Network locations are not supported yet.");
        string path = PathSafety.Canonical(location);
        if (new DriveInfo(Path.GetPathRoot(path)!).DriveType == DriveType.Network)
            throw new IOException("Network drives are not supported by this local preview.");
        PathSafety.RejectLinks(path);
        return path;
    }
    public Task<DirectorySnapshot> ReadAsync(string location, bool showHidden = false, CancellationToken cancellation = default) => Task.Run(() =>
    {
        string path = ValidateLocation(location); var entries = new List<FileEntry>(); int skipped = 0, visited = 0; bool truncated = false;
        var options = new EnumerationOptions { RecurseSubdirectories = false, IgnoreInaccessible = false,
            AttributesToSkip = showHidden ? 0 : FileAttributes.Hidden | FileAttributes.System, ReturnSpecialDirectories = false };
        foreach (var item in new DirectoryInfo(path).EnumerateFileSystemInfos("*", options))
        {
            cancellation.ThrowIfCancellationRequested();
            if (visited++ >= MaxEntries) { truncated = true; break; }
            try
            {
                var attributes = item.Attributes; bool directory = (attributes & FileAttributes.Directory) != 0;
                entries.Add(new(item.Name, item.FullName, directory, (attributes & FileAttributes.ReparsePoint) != 0,
                    !directory && item is FileInfo file ? file.Length : null, item.LastWriteTime, attributes));
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { skipped++; }
        }
        return new DirectorySnapshot(path, entries.OrderByDescending(e => e.IsDirectory)
            .ThenBy(e => e.Name, StringComparer.OrdinalIgnoreCase).ToArray(), truncated, skipped);
    }, cancellation);
}
