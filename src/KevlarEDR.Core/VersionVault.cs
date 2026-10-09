using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace KevlarEDR.Core;

public sealed class VersionVault
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private readonly VaultLimits limits;
    private readonly string metadata;
    private readonly string blobs;
    public string Root { get; }
    public VaultLimits Limits => limits;

    public VersionVault(string root, VaultLimits? limits = null)
    {
        Root = PathSafety.Canonical(root);
        PathSafety.RejectLinks(Root);
        this.limits = limits ?? new();
        if (this.limits.MaxFileBytes <= 0 || this.limits.MaxVaultBytes <= 0 || this.limits.MaxVersions <= 0)
            throw new ArgumentOutOfRangeException(nameof(limits));
        Directory.CreateDirectory(Root);
        blobs = Path.Combine(Root, "blobs");
        PathSafety.RejectLinks(blobs);
        Directory.CreateDirectory(blobs);
        metadata = Path.Combine(Root, "state.json");
    }

    private async Task<FileStream> LockAsync(CancellationToken token)
    {
        var deadline = DateTime.UtcNow.AddSeconds(15);
        while (true)
        {
            token.ThrowIfCancellationRequested();
            PathSafety.RejectLinks(Root);
            string lockPath = Path.Combine(Root, "vault.lock");
            PathSafety.RejectLinks(lockPath);
            try { return new FileStream(lockPath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None); }
            catch (IOException) when (DateTime.UtcNow < deadline) { await Task.Delay(50, token); }
        }
    }
    private VaultState Load()
    {
        PathSafety.RejectLinks(metadata);
        if (!File.Exists(metadata)) return new();
        if (new FileInfo(metadata).Length > 16 * 1024 * 1024) throw new InvalidDataException("Metadata exceeds its size limit.");
        var state = JsonSerializer.Deserialize<VaultState>(File.ReadAllText(metadata), JsonOptions)
            ?? throw new InvalidDataException("Recovery metadata is empty.");
        if (state.SchemaVersion != 1 || state.ProtectedFolders is null || state.Versions is null || state.Activity is null)
            throw new InvalidDataException("Unsupported or malformed recovery metadata.");
        if (state.Versions.Count > limits.MaxVersions || state.Activity.Count > 500 || state.ProtectedFolders.Count > 100)
            throw new InvalidDataException("Metadata exceeds supported record limits.");
        foreach (var version in state.Versions)
        {
            if (version is null || !Guid.TryParseExact(version.Id, "N", out _) || version.Size < 0 ||
                string.IsNullOrEmpty(version.SourcePath) || !Path.IsPathFullyQualified(version.SourcePath) ||
                version.SourcePath.Length > 32760 || string.IsNullOrWhiteSpace(version.Reason) || version.Reason.Length > 256 ||
                string.IsNullOrEmpty(version.Sha256) || !Regex.IsMatch(version.Sha256, "^[A-F0-9]{64}$"))
                throw new InvalidDataException("Invalid version record.");
            PathSafety.RequireFileName(version.FileName);
        }
        if (state.Versions.Select(v => v.Id).Distinct().Count() != state.Versions.Count)
            throw new InvalidDataException("Duplicate version identities in metadata.");
        if (state.ProtectedFolders.Any(p => string.IsNullOrWhiteSpace(p) || !Path.IsPathFullyQualified(p)))
            throw new InvalidDataException("Invalid protected-folder metadata.");
        return state;
    }
    private void Save(VaultState state)
    {
        PathSafety.RejectLinks(metadata);
        string temp = Path.Combine(Root, Guid.NewGuid().ToString("N") + ".json.tmp");
        try
        {
            byte[] serialized = JsonSerializer.SerializeToUtf8Bytes(state, JsonOptions);
            if (serialized.Length > 16 * 1024 * 1024) throw new IOException("Metadata capacity reached; earlier records retained.");
            using (var stream = new FileStream(temp, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                stream.Write(serialized);
                stream.Flush(true);
            }
            File.Move(temp, metadata, true);
        }
        finally { if (File.Exists(temp)) File.Delete(temp); }
    }
    private static void Event(VaultState state, string operation, string outcome, string detail)
    {
        state.Activity.Insert(0, new(DateTimeOffset.UtcNow, operation, outcome, detail.Length > 2048 ? detail[..2048] : detail));
        if (state.Activity.Count > 500) state.Activity.RemoveRange(500, state.Activity.Count - 500);
    }
    public async Task<VaultState> ReadAsync(CancellationToken token = default)
    {
        using var held = await LockAsync(token);
        return Load();
    }
    public async Task RecordAsync(string operation, string outcome, string detail, CancellationToken token = default)
    {
        using var held = await LockAsync(token);
        var state = Load(); Event(state, operation, outcome, detail); Save(state);
    }
    public async Task StoreScanAsync(string target, ScanResult result, CancellationToken token = default)
    {
        using var held = await LockAsync(token);
        var state = Load(); state.LastScan = result; state.LastScanTarget = target;
        Event(state, "Scan", result.Status, target + "; exit code " + result.ExitCode); Save(state);
    }
    public string ValidateFolder(string folder)
    {
        folder = PathSafety.Canonical(folder);
        PathSafety.RejectLinks(folder);
        if (!Directory.Exists(folder)) throw new DirectoryNotFoundException("The folder does not exist.");
        if (PathSafety.Within(folder, Root) || PathSafety.Within(Root, folder))
            throw new IOException("Choose a folder separate from the recovery vault.");
        if (folder.Equals(Path.TrimEndingDirectorySeparator(Path.GetPathRoot(folder) ?? ""), StringComparison.OrdinalIgnoreCase))
            throw new IOException("Protect selected folders rather than an entire volume.");
        string windows = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
        if (windows.Length > 0 && PathSafety.Within(folder, PathSafety.Canonical(windows)))
            throw new IOException("System folders cannot be protected by this prototype.");
        return folder;
    }
    public async Task AddFolderAsync(string folder, CancellationToken token = default)
    {
        folder = ValidateFolder(folder);
        using var held = await LockAsync(token);
        var state = Load();
        if (state.ProtectedFolders.Any(p => PathSafety.Within(folder, p) || PathSafety.Within(p, folder)))
            throw new IOException("This folder overlaps an existing protected folder.");
        if (state.ProtectedFolders.Count >= 100) throw new IOException("Protected-folder limit reached.");
        state.ProtectedFolders.Add(folder);
        Event(state, "Folder", "Added", folder); Save(state);
    }
    public async Task RemoveFolderAsync(string folder, CancellationToken token = default)
    {
        using var held = await LockAsync(token);
        var state = Load();
        state.ProtectedFolders.RemoveAll(p => p.Equals(folder, StringComparison.OrdinalIgnoreCase));
        Event(state, "Folder", "Removed", folder + "; existing versions retained."); Save(state);
    }
    public async Task<FileVersion?> CaptureAsync(string source, string reason, CancellationToken token = default)
    {
        if (string.IsNullOrWhiteSpace(reason) || reason.Length > 256) throw new ArgumentException("Invalid capture reason.");
        source = PathSafety.Canonical(source); PathSafety.RejectLinks(source);
        if (PathSafety.Within(source, Root)) throw new IOException("The recovery vault cannot capture itself.");
        if (!File.Exists(source)) throw new FileNotFoundException("Source is unavailable.", source);
        using var held = await LockAsync(token);
        PathSafety.RejectLinks(blobs);
        var state = Load();
        var before = new FileInfo(source);
        long beforeSize = before.Length; DateTime beforeTime = before.LastWriteTimeUtc;
        if (beforeSize > limits.MaxFileBytes) throw new IOException("File exceeds the capture size limit.");
        string temp = Path.Combine(blobs, Guid.NewGuid().ToString("N") + ".tmp");
        try
        {
            string hash; long bytes = 0;
            using (var digest = IncrementalHash.CreateHash(HashAlgorithmName.SHA256))
            {
                await using var input = new FileStream(source, FileMode.Open, FileAccess.Read,
                    FileShare.ReadWrite | FileShare.Delete, 65536, FileOptions.Asynchronous | FileOptions.SequentialScan);
                await using var output = new FileStream(temp, FileMode.CreateNew, FileAccess.Write, FileShare.None,
                    65536, FileOptions.Asynchronous);
                byte[] buffer = new byte[65536]; int read;
                while ((read = await input.ReadAsync(buffer, token)) != 0)
                {
                    bytes += read;
                    if (bytes > limits.MaxFileBytes) throw new IOException("File grew beyond the capture limit.");
                    digest.AppendData(buffer, 0, read); await output.WriteAsync(buffer.AsMemory(0, read), token);
                }
                output.Flush(true);
                hash = Convert.ToHexString(digest.GetHashAndReset());
            }
            var after = new FileInfo(source);
            if (!after.Exists || after.Length != beforeSize || after.LastWriteTimeUtc != beforeTime || bytes != beforeSize)
                throw new IOException("Source changed during capture; retry when the file is stable.");
            var latest = state.Versions.LastOrDefault(v => v.SourcePath.Equals(source, StringComparison.OrdinalIgnoreCase));
            if (latest?.Sha256 == hash)
            {
                await VerifyAsync(Path.Combine(blobs, hash + ".blob"), hash, bytes, token);
                return null;
            }
            if (state.Versions.Count >= limits.MaxVersions) throw new IOException("Version limit reached; earlier versions retained.");
            string blob = Path.Combine(blobs, hash + ".blob"); PathSafety.RejectLinks(blob);
            if (!File.Exists(blob))
            {
                long used = Directory.EnumerateFiles(blobs, "*.blob").Sum(p => new FileInfo(p).Length);
                if (used > limits.MaxVaultBytes - bytes) throw new IOException("Vault capacity reached; earlier versions retained.");
                File.Move(temp, blob);
            }
            else await VerifyAsync(blob, hash, bytes, token);
            var version = new FileVersion(Guid.NewGuid().ToString("N"), source, Path.GetFileName(source),
                DateTimeOffset.UtcNow, bytes, hash, reason);
            state.Versions.Add(version); Event(state, "Capture", "Saved", source); Save(state);
            return version;
        }
        finally { if (File.Exists(temp)) File.Delete(temp); }
    }
    private static async Task VerifyAsync(string path, string hash, long size, CancellationToken token)
    {
        PathSafety.RejectLinks(path);
        if (!File.Exists(path) || new FileInfo(path).Length != size) throw new InvalidDataException("Version is missing or its size is invalid.");
        await using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 65536, FileOptions.Asynchronous);
        string actual = Convert.ToHexString(await SHA256.HashDataAsync(stream, token));
        if (actual != hash) throw new InvalidDataException("Integrity verification failed; restore refused.");
    }
    public async Task<string> RestoreAsync(string versionId, string destination, CancellationToken token = default)
    {
        destination = PathSafety.Canonical(destination); PathSafety.RejectLinks(destination);
        if (!Directory.Exists(destination)) throw new DirectoryNotFoundException("Choose an existing restore folder.");
        if (PathSafety.Within(destination, Root)) throw new IOException("Do not restore into the vault.");
        using var held = await LockAsync(token);
        var state = Load();
        var version = state.Versions.SingleOrDefault(v => v.Id == versionId)
            ?? throw new InvalidDataException("Version not found.");
        string blob = Path.Combine(blobs, version.Sha256 + ".blob");
        await VerifyAsync(blob, version.Sha256, version.Size, token);
        string temp = Path.Combine(destination, ".kevlar-" + Guid.NewGuid().ToString("N") + ".tmp");
        try
        {
            await using (var input = new FileStream(blob, FileMode.Open, FileAccess.Read, FileShare.Read, 65536, FileOptions.Asynchronous))
            await using (var output = new FileStream(temp, FileMode.CreateNew, FileAccess.Write, FileShare.None, 65536, FileOptions.Asynchronous))
            { await input.CopyToAsync(output, token); output.Flush(true); }
            await VerifyAsync(temp, version.Sha256, version.Size, token);
            string result = Path.Combine(destination, version.FileName);
            if (File.Exists(result) || Directory.Exists(result))
                result = Path.Combine(destination, Path.GetFileNameWithoutExtension(version.FileName) +
                    "-recovered-" + Guid.NewGuid().ToString("N")[..8] + Path.GetExtension(version.FileName));
            PathSafety.RejectLinks(result);
            File.Move(temp, result, false);
            Event(state, "Restore", "Succeeded", result); Save(state);
            return result;
        }
        finally { if (File.Exists(temp)) File.Delete(temp); }
    }
    public async Task<CaptureSummary> CaptureFolderAsync(string folder, CancellationToken token = default)
    {
        folder = ValidateFolder(folder);
        int saved = 0, unchanged = 0, failed = 0;
        var options = new EnumerationOptions { RecurseSubdirectories = true, IgnoreInaccessible = false,
            AttributesToSkip = FileAttributes.ReparsePoint | FileAttributes.System };
        try
        {
            foreach (string file in Directory.EnumerateFiles(folder, "*", options))
            {
                token.ThrowIfCancellationRequested();
                try
                {
                    if (await CaptureAsync(file, "Baseline", token) is null) unchanged++; else saved++;
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                { failed++; await RecordAsync("Capture", "Failed", file + ": " + ex.Message, token); }
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        { failed++; await RecordAsync("Baseline", "Incomplete", ex.Message, token); }
        await RecordAsync("Baseline", failed == 0 ? "Completed" : "Partial",
            $"{folder}: {saved} saved, {unchanged} unchanged, {failed} failed.", token);
        return new(saved, unchanged, failed);
    }
}
