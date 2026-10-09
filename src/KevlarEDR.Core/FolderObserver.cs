using System.Threading.Channels;

namespace KevlarEDR.Core;

public sealed class FolderObserver : IAsyncDisposable
{
    private readonly VersionVault vault;
    private readonly CancellationTokenSource stop = new();
    private readonly Channel<string> queue = Channel.CreateBounded<string>(new BoundedChannelOptions(256)
        { FullMode = BoundedChannelFullMode.Wait, SingleReader = true });
    private readonly List<FileSystemWatcher> watchers = [];
    private readonly Task worker;
    private int dropped;
    public event Action<string>? HealthChanged;
    public int WatchedFolders => watchers.Count;
    public string Health { get; private set; } = "No folders monitored";
    public FolderObserver(VersionVault vault)
    {
        this.vault = vault;
        worker = Task.Run(WorkAsync);
    }
    private void Report(string message) { Health = message; HealthChanged?.Invoke(message); }
    public void Configure(IEnumerable<string> folders)
    {
        foreach (var watcher in watchers) watcher.Dispose(); watchers.Clear();
        int failures = 0;
        foreach (string folder in folders)
        {
            try
            {
                vault.ValidateFolder(folder);
                var watcher = new FileSystemWatcher(folder) { IncludeSubdirectories = true,
                    NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.Size };
                watcher.Changed += Changed; watcher.Created += Changed;
                watcher.Renamed += (_, e) => Enqueue(e.FullPath);
                watcher.Error += (_, _) => { Interlocked.Increment(ref dropped); Report("Observation degraded; capture a new baseline."); };
                watcher.EnableRaisingEvents = true; watchers.Add(watcher);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException)
            { failures++; Report("A folder could not be monitored: " + ex.Message); }
        }
        if (Volatile.Read(ref dropped) == 0 && failures == 0)
            Report(watchers.Count == 0 ? "No folders monitored" : $"Observing {watchers.Count} folder(s) while app is open");
    }
    private void Changed(object sender, FileSystemEventArgs e) => Enqueue(e.FullPath);
    private void Enqueue(string path)
    {
        if (!queue.Writer.TryWrite(path))
        { Interlocked.Increment(ref dropped); Report("Observation queue full; capture a new baseline."); }
    }
    private async Task WorkAsync()
    {
        try
        {
            await foreach (string first in queue.Reader.ReadAllAsync(stop.Token))
            {
                await Task.Delay(350, stop.Token);
                var batch = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { first };
                while (queue.Reader.TryRead(out string? next)) batch.Add(next);
                foreach (string file in batch)
                {
                    if (!File.Exists(file)) continue;
                    var configured = await vault.ReadAsync(stop.Token);
                    if (!configured.ProtectedFolders.Any(folder => PathSafety.Within(PathSafety.Canonical(file), folder))) continue;
                    try { await vault.CaptureAsync(file, "Observed change", stop.Token); }
                    catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                    { await vault.RecordAsync("Observation", "Failed", file + ": " + ex.Message, stop.Token); }
                }
                int lost = Interlocked.Exchange(ref dropped, 0);
                if (lost > 0) await vault.RecordAsync("Observation", "Degraded", $"{lost} notifications/observer errors; capture a new baseline.", stop.Token);
            }
        }
        catch (OperationCanceledException) when (stop.IsCancellationRequested) { }
        catch (Exception ex) { Report("Observation stopped: " + ex.Message); }
    }
    public async ValueTask DisposeAsync()
    {
        foreach (var watcher in watchers) watcher.Dispose(); watchers.Clear();
        stop.Cancel(); queue.Writer.TryComplete(); await worker.ConfigureAwait(false); stop.Dispose();
    }
}
