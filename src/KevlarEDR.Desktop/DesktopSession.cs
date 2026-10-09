using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Windows;
using System.Windows.Interop;
using Forms = System.Windows.Forms;
using Drawing = System.Drawing;

namespace KevlarEDR.Desktop;

internal sealed class DesktopSession : IDisposable
{
    private readonly MainWindow window;
    private readonly HwndSource source;
    private readonly Forms.NotifyIcon tray;
    private readonly Drawing.Icon icon;
    private readonly uint showMessage, exitMessage;
    public static string UserKey => WindowsIdentity.GetCurrent().User!.Value;
    public static void SendToExisting(bool exit)
    {
        uint message = RegisterWindowMessage("KevlarEDR." + (exit ? "Exit." : "Show.") + UserKey);
        PostMessage(new nint(0xffff), message, 0, 0);
    }
    public DesktopSession(MainWindow window)
    {
        this.window = window;
        showMessage = RegisterWindowMessage("KevlarEDR.Show." + UserKey);
        exitMessage = RegisterWindowMessage("KevlarEDR.Exit." + UserKey);
        source = HwndSource.FromHwnd(new WindowInteropHelper(window).EnsureHandle())!;
        source.AddHook(Hook);
        icon = Drawing.Icon.ExtractAssociatedIcon(Environment.ProcessPath!) ?? (Drawing.Icon)Drawing.SystemIcons.Application.Clone();
        var menu = new Forms.ContextMenuStrip();
        menu.Items.Add("Open KevlarEDR", null, (_, _) => Show());
        menu.Items.Add("Quit (stop monitoring)", null, (_, _) => window.Quit());
        tray = new Forms.NotifyIcon { Icon = icon, Text = "KevlarEDR • device detection running", ContextMenuStrip = menu, Visible = true };
        tray.DoubleClick += (_, _) => Show(); tray.BalloonTipClicked += (_, _) => Show();
        window.DeviceNotification += message => Notify(message);
        window.TrayHidden += () => Notify("Still running in the notification area. Double-click the shield to open; right-click → Quit to stop monitoring.");
    }
    private void Show() { window.Show(); if (window.WindowState == WindowState.Minimized) window.WindowState = WindowState.Normal; window.Activate(); }
    private void Notify(string message)
    { tray.ShowBalloonTip(5000, "KevlarEDR", message, Forms.ToolTipIcon.Info); }
    private nint Hook(nint hwnd, int message, nint wParam, nint lParam, ref bool handled)
    {
        if ((uint)message == showMessage) { Show(); handled = true; }
        if ((uint)message == exitMessage) { window.Dispatcher.BeginInvoke(window.Quit); handled = true; }
        return 0;
    }
    public void Dispose() { source.RemoveHook(Hook); tray.Visible = false; tray.ContextMenuStrip?.Dispose(); tray.Dispose(); icon.Dispose(); }
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern uint RegisterWindowMessage(string message);
    [DllImport("user32.dll", SetLastError = true)] [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool PostMessage(nint hwnd, uint message, nint wParam, nint lParam);
}
