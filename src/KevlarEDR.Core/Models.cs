namespace KevlarEDR.Core;

public sealed record FileVersion(string Id, string SourcePath, string FileName,
    DateTimeOffset CapturedAt, long Size, string Sha256, string Reason);
public sealed record ActivityEntry(DateTimeOffset At, string Operation, string Outcome, string Detail);
public sealed class VaultState
{
    public int SchemaVersion { get; set; } = 1;
    public List<string> ProtectedFolders { get; set; } = [];
    public List<FileVersion> Versions { get; set; } = [];
    public List<ActivityEntry> Activity { get; set; } = [];
    public ScanResult? LastScan { get; set; }
    public string? LastScanTarget { get; set; }
}
public sealed record VaultLimits(long MaxFileBytes = 32 * 1024 * 1024,
    long MaxVaultBytes = 512 * 1024 * 1024, int MaxVersions = 5000);
public sealed record CaptureSummary(int Saved, int Unchanged, int Failed);
public sealed record ScanResult(string Status, int? ExitCode, string Output, DateTimeOffset CompletedAt);

public static class PathSafety
{
    public static string Canonical(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("Choose a path.");
        return Path.TrimEndingDirectorySeparator(Path.GetFullPath(path));
    }
    public static bool Within(string candidate, string parent) =>
        candidate.Equals(parent, StringComparison.OrdinalIgnoreCase) ||
        candidate.StartsWith(parent + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
    public static void RejectLinks(string path)
    {
        string? current = Canonical(path);
        while (current is not null)
        {
            if ((File.Exists(current) || Directory.Exists(current)) &&
                (File.GetAttributes(current) & FileAttributes.ReparsePoint) != 0)
                throw new IOException("Linked/reparse-point paths are not supported: " + current);
            current = Path.GetDirectoryName(current);
        }
    }
    public static void RequireFileName(string name)
    {
        if (string.IsNullOrWhiteSpace(name) || name is "." or ".." ||
            name != Path.GetFileName(name) || name.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            throw new InvalidDataException("Invalid version filename.");
    }
}
