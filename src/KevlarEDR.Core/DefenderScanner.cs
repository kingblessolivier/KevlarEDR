using System.Diagnostics;
using System.Text;

namespace KevlarEDR.Core;

public sealed class DefenderScanner
{
    public string? Executable { get; }
    public DefenderScanner()
    {
        if (!OperatingSystem.IsWindows()) return;
        string platform = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            "Microsoft", "Windows Defender", "Platform");
        try
        {
            if (Directory.Exists(platform)) Executable = Directory.EnumerateDirectories(platform)
                .OrderByDescending(p => Version.TryParse(Path.GetFileName(p).Split('-')[0], out var v) ? v : new Version())
                .Select(p => Path.Combine(p, "MpCmdRun.exe")).FirstOrDefault(File.Exists);
            string fallback = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Windows Defender", "MpCmdRun.exe");
            Executable ??= File.Exists(fallback) ? fallback : null;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { Executable = null; }
    }
    public static void ValidateTarget(string target)
    {
        target = PathSafety.Canonical(target); PathSafety.RejectLinks(target);
        if (!File.Exists(target) && !Directory.Exists(target)) throw new FileNotFoundException("Scan target does not exist.");
    }
    public async Task<ScanResult> ScanAsync(string target, CancellationToken token = default)
    {
        ValidateTarget(target);
        if (Executable is null) return new("Unavailable", null, "Microsoft Defender scan executable was not found.", DateTimeOffset.UtcNow);
        var start = new ProcessStartInfo(Executable) { UseShellExecute = false, RedirectStandardOutput = true,
            RedirectStandardError = true, CreateNoWindow = true };
        foreach (string arg in new[] { "-Scan", "-ScanType", "3", "-File", Path.GetFullPath(target), "-DisableRemediation" })
            start.ArgumentList.Add(arg);
        using var process = new Process { StartInfo = start };
        if (!process.Start()) throw new IOException("The scanner could not start.");
        Task<string> output = ReadBoundedAsync(process.StandardOutput, token);
        Task<string> error = ReadBoundedAsync(process.StandardError, token);
        try { await process.WaitForExitAsync(token); }
        catch (OperationCanceledException)
        {
            // Only this launcher is stopped; the provider may continue its scan.
            if (!process.HasExited) process.Kill();
            throw;
        }
        string combined = await output + Environment.NewLine + await error;
        string status = process.ExitCode == 0 ? "Completed — review provider output" : "Failed or requires action";
        return new(status, process.ExitCode, combined.Trim(), DateTimeOffset.UtcNow);
    }
    private static async Task<string> ReadBoundedAsync(StreamReader reader, CancellationToken token)
    {
        var text = new StringBuilder(); char[] buffer = new char[4096]; int count;
        while ((count = await reader.ReadAsync(buffer.AsMemory(), token)) > 0)
        {
            text.Append(buffer, 0, count);
            if (text.Length > 32768) text.Remove(0, text.Length - 32768);
        }
        return text.ToString();
    }
}
