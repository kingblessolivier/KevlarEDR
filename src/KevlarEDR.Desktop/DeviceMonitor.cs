using System.IO;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;
using KevlarEDR.Core;

namespace KevlarEDR.Desktop;

/// <summary>Windows volume broadcasts plus periodic reconciliation for missed/delayed mounts.</summary>
internal sealed class DeviceMonitor : IDisposable
{
    private readonly HwndSource source;
    private readonly DispatcherTimer timer;
    private readonly VolumeTracker tracker = new();
    private readonly Func<IReadOnlyList<LocalVolume>> inventory;
    private readonly Func<IReadOnlyList<VolumeChange>, Task> changed;
    private readonly Action<string> failed;
    private bool reading, disposed, pending;
    public DeviceMonitor(Window window, Func<IReadOnlyList<VolumeChange>, Task> changed, Action<string> failed,
        Func<IReadOnlyList<LocalVolume>>? inventory = null)
    {
        this.changed = changed; this.failed = failed; this.inventory = inventory ?? VolumeTracker.Read;
        source = HwndSource.FromHwnd(new WindowInteropHelper(window).EnsureHandle())!;
        source.AddHook(Hook);
        timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(3) };
        timer.Tick += async (_, _) => await ReconcileAsync();
    }
    public async Task StartAsync() { await ReconcileAsync(); if (!disposed) timer.Start(); }
    private nint Hook(nint hwnd, int message, nint wParam, nint lParam, ref bool handled)
    {
        // Never dereference broadcast payloads. Re-read actual local volumes off the UI thread.
        if (message == 0x0219 && (wParam == 0x8000 || wParam == 0x8004 || wParam == 0x0007))
            source.Dispatcher.BeginInvoke(async () => await ReconcileAsync());
        return 0;
    }
    public async Task ReconcileAsync()
    {
        if (disposed) return;
        if (reading) { pending = true; return; }
        reading = true;
        try
        {
            do
            {
                pending = false;
                var volumes = await Task.Run(inventory);
                if (disposed) return;
                var changes = tracker.Update(volumes);
                if (changes.Count > 0) await changed(changes);
            } while (pending && !disposed);
        }
        catch (Exception ex)
        { if (!disposed) failed("Device detection could not refresh: " + ex.Message); }
        finally { reading = false; }
    }
    public void Dispose() { disposed = true; timer.Stop(); source.RemoveHook(Hook); }
    // A smoke test sends to this application's window only, never to other apps.
    internal void SimulateArrivalMessage() => SendMessage(source.Handle, 0x0219, 0x8000, 0);
    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern nint SendMessage(nint hwnd, int message, nint wParam, nint lParam);
}
