using System.Text;
using KevlarEDR.Core;

if (args.Length == 2 && args[0] == "--hold-lock")
{
    using var lockFile = new FileStream(args[1], FileMode.Open, FileAccess.ReadWrite, FileShare.None);
    Console.WriteLine("LOCKED");
    await Task.Delay(1000);
    return 0;
}
string fixtureRoot = Path.Combine(Path.GetTempPath(), "KevlarEDR-validation-" + Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(fixtureRoot);
var cases = new List<string>();
void Assert(bool condition, string name) { if (!condition) throw new Exception("FAILED: " + name); cases.Add(name); Console.WriteLine("PASS: " + name); }
async Task MustFail(Func<Task> operation, string name)
{
    bool rejected = false;
    try { await operation(); } catch (Exception ex) when (ex is IOException or InvalidDataException or UnauthorizedAccessException or ArgumentException or System.Text.Json.JsonException) { rejected = true; }
    Assert(rejected, name);
}
try
{
    string source = Path.Combine(fixtureRoot, "source"), restored = Path.Combine(fixtureRoot, "restored");
    Directory.CreateDirectory(source); Directory.CreateDirectory(restored);
    var vault = new VersionVault(Path.Combine(fixtureRoot, "vault"));
    await vault.AddFolderAsync(source);
    string document = Path.Combine(source, "report.txt");
    byte[] original = Encoding.UTF8.GetBytes("Original document — preserved before the simulation.");
    await File.WriteAllBytesAsync(document, original);
    var first = await vault.CaptureAsync(document, "Test") ?? throw new Exception("Initial capture missing.");
    Assert(await vault.CaptureAsync(document, "Test") is null, "Unchanged source is deduplicated");
    await File.WriteAllTextAsync(document, "SIMULATED overwritten content");
    var second = await vault.CaptureAsync(document, "Test");
    Assert(second is not null && second.Sha256 != first.Sha256, "Overwrite creates a different version");
    File.Delete(document);
    string recovered = await vault.RestoreAsync(first.Id, restored);
    Assert((await File.ReadAllBytesAsync(recovered)).SequenceEqual(original), "Deleted original recovered byte-for-byte");
    await File.WriteAllTextAsync(recovered, "Existing destination must survive");
    string collision = await vault.RestoreAsync(first.Id, restored);
    Assert(collision != recovered && await File.ReadAllTextAsync(recovered) == "Existing destination must survive", "Restore never replaces an existing destination");
    var reopened = new VersionVault(vault.Root);
    Assert((await reopened.ReadAsync()).Versions.Count == 2, "Version metadata survives reopening");
    Assert((await File.ReadAllBytesAsync(await reopened.RestoreAsync(first.Id, restored))).SequenceEqual(original), "Reopened vault restores verified bytes");
    byte[] corrupted = (byte[])original.Clone(); corrupted[0] ^= 0xFF;
    await File.WriteAllBytesAsync(Path.Combine(vault.Root, "blobs", first.Sha256 + ".blob"), corrupted);
    int before = Directory.GetFiles(restored).Length;
    await MustFail(() => vault.RestoreAsync(first.Id, restored), "Corrupted blob rejected");
    Assert(Directory.GetFiles(restored).Length == before, "Failed integrity check creates no restored file");
    await MustFail(() => vault.AddFolderAsync(vault.Root), "Vault cannot protect itself");
    await MustFail(() => vault.AddFolderAsync(fixtureRoot), "Vault ancestor cannot be selected");
    await MustFail(() => vault.AddFolderAsync(source), "Overlapping folder rejected");
    await MustFail(() => vault.RestoreAsync(second!.Id, vault.Root), "Restore into vault rejected");
    await MustFail(() => vault.CaptureAsync(Path.Combine(source, "missing.txt"), "Test"), "Missing source rejected");
    string limitedSource = Path.Combine(fixtureRoot, "small.txt"); await File.WriteAllTextAsync(limitedSource, "1234");
    var limited = new VersionVault(Path.Combine(fixtureRoot, "limited"), new(8, 8, 10));
    var small = await limited.CaptureAsync(limitedSource, "Test") ?? throw new Exception("Small capture failed.");
    await File.WriteAllTextAsync(limitedSource, "123456789");
    await MustFail(() => limited.CaptureAsync(limitedSource, "Test"), "Oversized file rejected");
    await File.WriteAllTextAsync(limitedSource, "abcdefgh");
    await MustFail(() => limited.CaptureAsync(limitedSource, "Test"), "Full vault refuses new blob");
    Assert(await File.ReadAllTextAsync(await limited.RestoreAsync(small.Id, restored)) == "1234", "Capacity failure retains earlier valid version");
    var recordLimited = new VersionVault(Path.Combine(fixtureRoot, "record-limited"), new(16, 64, 1));
    await File.WriteAllTextAsync(limitedSource, "first");
    await recordLimited.CaptureAsync(limitedSource, "Test");
    await File.WriteAllTextAsync(limitedSource, "second");
    await MustFail(() => recordLimited.CaptureAsync(limitedSource, "Test"), "Version-record limit rejects additional versions");
    using (var exclusive = new FileStream(limitedSource, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
        await MustFail(() => vault.CaptureAsync(limitedSource, "Test"), "Exclusively locked source rejected without a partial version");
    var concurrent = new VersionVault(Path.Combine(fixtureRoot, "concurrent"));
    await File.WriteAllTextAsync(limitedSource, "concurrent original");
    var results = await Task.WhenAll(Enumerable.Range(0, 8).Select(_ => concurrent.CaptureAsync(limitedSource, "Concurrent")));
    Assert(results.Count(r => r is not null) == 1 && (await concurrent.ReadAsync()).Versions.Count == 1, "Concurrent captures serialize and deduplicate");
    using (var held = new FileStream(Path.Combine(concurrent.Root, "vault.lock"), FileMode.Open, FileAccess.ReadWrite, FileShare.None))
    {
        using var cancellation = new CancellationTokenSource(150);
        bool cancelled = false;
        try { await concurrent.ReadAsync(cancellation.Token); } catch (OperationCanceledException) { cancelled = true; }
        Assert(cancelled, "Locked vault wait honors cancellation");
    }
    var childStart = new System.Diagnostics.ProcessStartInfo(Environment.ProcessPath ?? throw new Exception("Process path unavailable."))
        { UseShellExecute = false, RedirectStandardOutput = true, CreateNoWindow = true };
    if (Path.GetFileNameWithoutExtension(childStart.FileName).Equals("dotnet", StringComparison.OrdinalIgnoreCase))
        childStart.ArgumentList.Add(System.Reflection.Assembly.GetExecutingAssembly().Location);
    childStart.ArgumentList.Add("--hold-lock"); childStart.ArgumentList.Add(Path.Combine(concurrent.Root, "vault.lock"));
    using (var child = System.Diagnostics.Process.Start(childStart) ?? throw new Exception("Lock-test child could not start."))
    {
        Assert(await child.StandardOutput.ReadLineAsync() == "LOCKED", "Independent process acquired vault lock");
        using var cancellation = new CancellationTokenSource(150);
        bool cancelled = false;
        try { await concurrent.ReadAsync(cancellation.Token); } catch (OperationCanceledException) { cancelled = true; }
        Assert(cancelled, "Vault respects an independent process lock");
        await child.WaitForExitAsync();
    }
    await using (var observer = new FolderObserver(concurrent))
    {
        await concurrent.AddFolderAsync(source);
        observer.Configure([source]);
        string observed = Path.Combine(source, "observed.txt"); await File.WriteAllTextAsync(observed, "Observed benign fixture");
        bool captured = false;
        for (int attempt = 0; attempt < 50; attempt++)
        { await Task.Delay(100); if ((await concurrent.ReadAsync()).Versions.Any(v => v.SourcePath == observed)) { captured = true; break; } }
        Assert(captured, "File-change observer captures a readable new file");
        await concurrent.RemoveFolderAsync(source);
        string unselected = Path.Combine(source, "after-removal.txt");
        await File.WriteAllTextAsync(unselected, "This folder is no longer selected.");
        await Task.Delay(750);
        Assert(!(await concurrent.ReadAsync()).Versions.Any(v => v.SourcePath == unselected), "Observer does not capture a removed folder's queued changes");
    }
    {
        string link = Path.Combine(fixtureRoot, "linked-source");
        try { Directory.CreateSymbolicLink(link, source); }
        catch (IOException ex) when (OperatingSystem.IsWindows() && (ex.HResult & 0xFFFF) == 1314)
        {
            var start = new System.Diagnostics.ProcessStartInfo("cmd.exe") { UseShellExecute = false, CreateNoWindow = true };
            foreach (string argument in new[] { "/c", "mklink", "/J", link, source }) start.ArgumentList.Add(argument);
            using var process = System.Diagnostics.Process.Start(start) ?? throw new Exception("Could not create junction fixture.");
            await process.WaitForExitAsync();
            if (process.ExitCode != 0) throw new Exception("Junction fixture creation failed.");
        }
        await MustFail(() => vault.AddFolderAsync(link), "Reparse-point source rejected");
        await MustFail(() => vault.RestoreAsync(second!.Id, link), "Reparse-point destination rejected");
        await MustFail(() => new LocalFileBrowser().ReadAsync(link), "Browser does not traverse linked locations");
        Assert((await new LocalFileBrowser().ReadAsync(fixtureRoot)).Entries.Any(e => e.FullPath == link && e.IsLink), "Browser identifies a linked folder without traversing it");
        Directory.Delete(link);
    }
    string invalidRoot = Path.Combine(fixtureRoot, "invalid"); Directory.CreateDirectory(invalidRoot);
    await File.WriteAllTextAsync(Path.Combine(invalidRoot, "state.json"), "{\"SchemaVersion\":99}");
    await MustFail(() => new VersionVault(invalidRoot).ReadAsync(), "Unsupported metadata schema rejected");
    await File.WriteAllTextAsync(Path.Combine(invalidRoot, "state.json"), "not valid JSON");
    await MustFail(() => new VersionVault(invalidRoot).ReadAsync(), "Malformed metadata rejected explicitly");
    bool missingTarget = false;
    try { DefenderScanner.ValidateTarget(Path.Combine(source, "nonexistent")); } catch (FileNotFoundException) { missingTarget = true; }
    Assert(missingTarget, "Missing scan target rejected before process execution");
    string binary = Path.Combine(source, "streaming.bin"); byte[] binaryBytes = new byte[2 * 1024 * 1024];
    System.Security.Cryptography.RandomNumberGenerator.Fill(binaryBytes); await File.WriteAllBytesAsync(binary, binaryBytes);
    var binaryVersion = await vault.CaptureAsync(binary, "Binary streaming") ?? throw new Exception("Binary capture failed.");
    Assert((await File.ReadAllBytesAsync(await vault.RestoreAsync(binaryVersion.Id, restored))).SequenceEqual(binaryBytes), "Binary streaming recovery preserves every byte");
    await vault.StoreScanAsync(source, new("Failed or requires action", 2, "Synthetic provider error: scan not completed", DateTimeOffset.UtcNow));
    Assert((await new VersionVault(vault.Root).ReadAsync()).LastScan?.Status == "Failed or requires action", "Failed scan status survives restart without becoming clean");
    string browseRoot = Path.Combine(fixtureRoot, "browse"); Directory.CreateDirectory(browseRoot);
    string browseChild = Path.Combine(browseRoot, "Reports"); Directory.CreateDirectory(browseChild);
    await File.WriteAllTextAsync(Path.Combine(browseChild, "nested.txt"), "Nested content");
    string browseFile = Path.Combine(browseRoot, "notes.txt"); await File.WriteAllTextAsync(browseFile, "Read-only browsing fixture");
    string browseHidden = Path.Combine(browseRoot, "hidden.txt"); await File.WriteAllTextAsync(browseHidden, "Hidden fixture"); File.SetAttributes(browseHidden, FileAttributes.Hidden);
    var fileBrowser = new LocalFileBrowser(); var directory = await fileBrowser.ReadAsync(browseRoot);
    Assert(directory.Entries.First().IsDirectory, "Browser lists folders before files");
    Assert(!directory.Entries.Any(e => e.Name == "nested.txt"), "Browser does not recursively enumerate folder contents");
    Assert(!directory.Entries.Any(e => e.Name == "hidden.txt"), "Browser hides hidden items by default");
    Assert((await fileBrowser.ReadAsync(browseRoot, true)).Entries.Any(e => e.Name == "hidden.txt"), "Hidden-items option includes hidden files");
    Assert(directory.Entries.Single(e => e.Name == "notes.txt").Size == new FileInfo(browseFile).Length, "Browser reports actual file size");
    Assert(await File.ReadAllTextAsync(browseFile) == "Read-only browsing fixture", "Browsing leaves source content unchanged");
    var bounded = await new LocalFileBrowser(1).ReadAsync(browseRoot);
    Assert(bounded.Entries.Count == 1 && bounded.Truncated, "Large directory limit is explicit and bounded");
    await MustFail(() => fileBrowser.ReadAsync(Path.Combine(browseRoot, "missing")), "Missing browse folder fails explicitly");
    await MustFail(() => fileBrowser.ReadAsync(@"\\example.invalid\share"), "UNC browsing rejected without a network connection");
    await MustFail(() => fileBrowser.ReadAsync("relative-folder"), "Ambiguous relative browse path rejected");
    using (var cancelledBrowse = new CancellationTokenSource())
    {
        cancelledBrowse.Cancel(); bool cancelled = false;
        try { await fileBrowser.ReadAsync(browseRoot, cancellation: cancelledBrowse.Token); } catch (OperationCanceledException) { cancelled = true; }
        Assert(cancelled, "Browser enumeration honors cancellation");
    }
    var volumeTracker = new VolumeTracker();
    var systemVolume = new LocalVolume(@"C:\", DriveType.Fixed, true, "System");
    var flashVolume = new LocalVolume(@"Z:\", DriveType.Removable, false, "Fixture USB");
    Assert(volumeTracker.Update([systemVolume]).Count == 0, "Existing drives establish a silent startup baseline");
    Assert(volumeTracker.Update([systemVolume, flashVolume]).Single().Kind == "Connected", "New removable drive is detected before media is ready");
    Assert(volumeTracker.Update([systemVolume, flashVolume]).Count == 0, "Repeated device notifications do not duplicate connection events");
    flashVolume = flashVolume with { Ready = true };
    Assert(volumeTracker.Update([systemVolume, flashVolume]).Single().Kind == "Ready", "Delayed mount readiness enables scan actions");
    Assert(volumeTracker.Update([systemVolume]).Single() is { Kind: "Removed", Volume.Root: @"Z:\" }, "Removed flash drive keeps its original identity in the event");
    Assert(volumeTracker.Update([systemVolume]).Count == 0, "Repeated removal notifications do not duplicate events");
    Assert(volumeTracker.Update([systemVolume, new(@"N:\", DriveType.Network, true, "Network")]).Count == 0, "Network drives are excluded from local connection detection");
    Assert(volumeTracker.Update([systemVolume, new(@"Y:\", DriveType.Fixed, true, "External disk")]).Single().Kind == "Connected", "New volumes reported as Fixed are detected too");
    Assert(volumeTracker.Update([systemVolume with { Ready = false }]).Any(c => c.Kind == "Unavailable"), "Media becoming unavailable is distinct from a missing volume");
    if (args.Length == 2 && args[0] == "--scan-smoke")
    {
        string scanFixture = Path.Combine(fixtureRoot, "scan-fixture"); Directory.CreateDirectory(scanFixture);
        await File.WriteAllTextAsync(Path.Combine(scanFixture, "benign.txt"), "Harmless KevlarEDR scanner integration fixture.");
        var result = await new DefenderScanner().ScanAsync(scanFixture);
        string report = Path.GetFullPath(args[1]); Directory.CreateDirectory(Path.GetDirectoryName(report)!);
        await File.WriteAllTextAsync(report, System.Text.Json.JsonSerializer.Serialize(result, new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
        Console.WriteLine("REAL DEFENDER SMOKE: " + result.Status + "; exit " + result.ExitCode);
    }
    Console.WriteLine($"SUCCESS: {cases.Count} validation scenarios. Fixtures: {fixtureRoot}");
    return 0;
}
catch (Exception ex) { Console.Error.WriteLine(ex); Console.Error.WriteLine("Fixtures retained: " + fixtureRoot); return 1; }
