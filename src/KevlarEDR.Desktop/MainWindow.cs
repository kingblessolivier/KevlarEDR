using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using KevlarEDR.Core;

namespace KevlarEDR.Desktop;

public sealed class MainWindow : Window
{
    private readonly VersionVault vault;
    private readonly FolderObserver observer;
    private readonly DefenderScanner scanner = new();
    private readonly StackPanel page = new() { Margin = new Thickness(28, 20, 28, 20) };
    private readonly TextBlock status = new() { Margin = new Thickness(18, 10, 18, 10), TextWrapping = TextWrapping.Wrap };
    private readonly ListBox navigation = new() { BorderThickness = new Thickness(0), Padding = new Thickness(10), Background = Brush("#ECEFF3") };
    private VaultState state = new();
    private bool busy;
    private bool closing;
    private readonly string[] pages = ["Overview", "Scans", "Devices", "Recovery", "Activity", "Settings"];
    public string? SmokeFailure { get; set; }
    private static SolidColorBrush Brush(string hex) => new((Color)ColorConverter.ConvertFromString(hex));
    public MainWindow(string root)
    {
        vault = new(root); observer = new(vault);
        Title = "KevlarEDR — Local protection prototype";
        Width = 1080; Height = 760; MinWidth = 760; MinHeight = 560;
        FontFamily = new FontFamily("Segoe UI"); FontSize = 14;
        Foreground = Brush("#182333"); Background = Brush("#F5F6F8");
        var shell = new DockPanel();
        var header = new Border { Background = Brushes.White, Padding = new Thickness(20, 15, 20, 15),
            BorderBrush = Brush("#DCE1E8"), BorderThickness = new Thickness(0, 0, 0, 1) };
        header.Child = Text("KevlarEDR     •     This computer: " + Environment.MachineName, 18, true);
        DockPanel.SetDock(header, Dock.Top); shell.Children.Add(header);
        var footer = new Border { Child = status, Background = Brushes.White, BorderBrush = Brush("#DCE1E8"),
            BorderThickness = new Thickness(0, 1, 0, 0) };
        DockPanel.SetDock(footer, Dock.Bottom); shell.Children.Add(footer);
        var navBorder = new Border { Width = 184, Child = navigation, Background = Brush("#ECEFF3") };
        DockPanel.SetDock(navBorder, Dock.Left); shell.Children.Add(navBorder);
        foreach (string label in pages) navigation.Items.Add(new ListBoxItem { Content = label, Padding = new Thickness(12, 12, 12, 12), Margin = new Thickness(0, 2, 0, 2) });
        navigation.SelectionChanged += (_, _) => Draw();
        shell.Children.Add(new ScrollViewer { Content = page, VerticalScrollBarVisibility = ScrollBarVisibility.Auto });
        Content = shell; navigation.SelectedIndex = 0;
        observer.HealthChanged += message => Dispatcher.BeginInvoke(() => { if (!closing && !busy) status.Text = message; });
        Loaded += async (_, _) => await RunAsync(async () =>
        {
            await ReloadAsync(); observer.Configure(state.ProtectedFolders);
            status.Text = "Prototype: monitoring runs only while this app is open. " + observer.Health;
        });
        Closing += (_, _) => { closing = true; observer.DisposeAsync().AsTask().GetAwaiter().GetResult(); };
    }
    private static TextBlock Text(string value, double size = 14, bool heading = false) => new()
    {
        Text = value, FontSize = size, FontWeight = heading ? FontWeights.SemiBold : FontWeights.Normal,
        TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 0, 0, 8)
    };
    private static Border Panel(UIElement content, bool warning = false) => new()
    {
        Child = content, Padding = new Thickness(18), Margin = new Thickness(0, 0, 0, 16), CornerRadius = new CornerRadius(8),
        Background = Brush(warning ? "#FFF5E4" : "#FFFFFF"), BorderBrush = Brush("#DCE1E8"), BorderThickness = new Thickness(1)
    };
    private Button Button(string label, Func<Task> operation, bool primary = false)
    {
        var button = new Button { Content = label, Padding = new Thickness(14, 9, 14, 9), Margin = new Thickness(0, 6, 10, 6),
            Background = primary ? Brush("#125CA7") : Brushes.White, Foreground = primary ? Brushes.White : Foreground,
            BorderBrush = Brush("#DCE1E8"), MinHeight = 36 };
        button.Click += async (_, _) => await RunAsync(operation);
        return button;
    }
    private async Task RunAsync(Func<Task> operation)
    {
        if (busy) return;
        busy = true; page.IsEnabled = false; navigation.IsEnabled = false; status.Text = "Working…";
        try { await operation(); if (!closing) await ReloadAsync(); }
        catch (Exception ex)
        {
            status.Text = ex.Message;
            if (!closing) MessageBox.Show(this, ex.Message, "KevlarEDR", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        finally { busy = false; page.IsEnabled = true; navigation.IsEnabled = true; }
    }
    private async Task ReloadAsync() { state = await vault.ReadAsync(); if (!closing) Draw(); }
    private void Draw()
    {
        if (navigation.SelectedIndex < 0) return;
        page.Children.Clear(); string selected = pages[navigation.SelectedIndex];
        page.Children.Add(Text(selected, 26, true));
        switch (selected)
        {
            case "Overview": Overview(); break;
            case "Scans": Scans(); break;
            case "Devices": Devices(); break;
            case "Recovery": Recovery(); break;
            case "Activity": Activity(); break;
            case "Settings": Settings(); break;
        }
    }
    private Task NavigateAsync(int index) { navigation.SelectedIndex = index; return Task.CompletedTask; }
    private void Overview()
    {
        var health = new StackPanel();
        health.Children.Add(Text(observer.Health, 18, true));
        health.Children.Add(Text("Version 0.1 preserves readable file versions after observed changes. It does not intercept writes or block malware."));
        page.Children.Add(Panel(health, true));
        page.Children.Add(Text($"{state.ProtectedFolders.Count} selected folder(s)    •    {state.Versions.Count} preserved version(s)", 18, true));
        page.Children.Add(Text(scanner.Executable is null ? "Defender scanner: unavailable" : "Defender scanner: found; scan execution depends on permissions and policy."));
        var actions = new WrapPanel();
        actions.Children.Add(Button("Scan a folder", () => NavigateAsync(1), true));
        actions.Children.Add(Button("Review devices", () => NavigateAsync(2)));
        actions.Children.Add(Button("Recover a file", () => NavigateAsync(3)));
        actions.Children.Add(Button("Protect a folder", () => NavigateAsync(5)));
        page.Children.Add(actions);
        page.Children.Add(Text("Recent activity", 18, true));
        foreach (var item in state.Activity.Take(5)) page.Children.Add(Text($"{item.At.LocalDateTime:g} · {item.Operation} · {item.Outcome}\n{item.Detail}"));
        page.Children.Add(Button("Refresh status", async () => { await ReloadAsync(); status.Text = observer.Health; }));
    }
    private static string? PickFolder(string title)
    {
        var dialog = new OpenFolderDialog { Title = title, Multiselect = false };
        return dialog.ShowDialog() == true ? dialog.FolderName : null;
    }
    private void Scans()
    {
        page.Children.Add(Text("Request a Microsoft Defender custom scan. Automatic remediation is disabled for this request; review the provider's outcome."));
        page.Children.Add(Button("Choose folder and scan", async () => { string? folder = PickFolder("Choose a folder to scan"); if (folder is not null) await ScanAsync(folder); }, true));
        page.Children.Add(Button("Choose file and scan", async () =>
        {
            var dialog = new OpenFileDialog { Title = "Choose a file to scan", CheckFileExists = true };
            if (dialog.ShowDialog(this) == true) await ScanAsync(dialog.FileName);
        }));
        page.Children.Add(Text("Last scan", 18, true));
        if (state.LastScan is { } scan)
        {
            page.Children.Add(Text($"{scan.Status}\nTarget: {state.LastScanTarget}\nExit code: {scan.ExitCode} · {scan.CompletedAt.LocalDateTime:g}"));
            page.Children.Add(new TextBox { Text = scan.Output, IsReadOnly = true, TextWrapping = TextWrapping.Wrap,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto, MaxHeight = 320, Padding = new Thickness(12) });
        }
        else page.Children.Add(Text("No scan has been requested from this app yet."));
        page.Children.Add(Text("If access is denied, review Defender permissions or company policy. Scan completion alone is not a safety verdict."));
    }
    private async Task ScanAsync(string target)
    {
        status.Text = "Defender is scanning. Large folders may take time.";
        ScanResult result;
        try { result = await scanner.ScanAsync(target); }
        catch (Exception ex) { result = new("Failed to start or complete", null, ex.Message, DateTimeOffset.UtcNow); }
        await vault.StoreScanAsync(target, result);
        navigation.SelectedIndex = 1;
        status.Text = result.Status + "; review the output.";
    }
    private void Devices()
    {
        page.Children.Add(Text("Windows-reported drives. External disks may appear as Fixed. Drive type does not establish trust."));
        page.Children.Add(Button("Refresh drives", () => { Draw(); status.Text = "Drive inventory refreshed."; return Task.CompletedTask; }));
        foreach (var drive in DriveInfo.GetDrives())
        {
            var content = new StackPanel(); string label;
            try { label = drive.IsReady ? $"{drive.Name}  {drive.VolumeLabel} · {drive.DriveType} · {drive.AvailableFreeSpace / (1024d * 1024 * 1024):F1} GiB free" : $"{drive.Name} · {drive.DriveType} · not ready"; }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { label = drive.Name + " · information unavailable"; }
            content.Children.Add(Text(label, 16, true));
            if (drive.IsReady) { string path = drive.RootDirectory.FullName; content.Children.Add(Button("Scan this drive", () => ScanAsync(path))); }
            page.Children.Add(Panel(content));
        }
        page.Children.Add(Text("USB blocking, read-only enforcement, device approval, and HID protection are future milestones."));
    }
    private sealed record VersionChoice(FileVersion Value)
    {
        public override string ToString() => $"{Value.FileName} · {Value.CapturedAt.LocalDateTime:g} · {Value.Size:N0} bytes\n{Value.SourcePath}";
    }
    private void Recovery()
    {
        page.Children.Add(Text("Select a preserved version. Integrity is checked when restoring; the destination's existing files are never replaced."));
        var list = new ListBox { MinHeight = 180, MaxHeight = 380, Padding = new Thickness(8), Margin = new Thickness(0, 8, 0, 10) };
        foreach (var version in state.Versions.OrderByDescending(v => v.CapturedAt)) list.Items.Add(new VersionChoice(version));
        page.Children.Add(list);
        page.Children.Add(Button("Restore selected version as a copy", async () =>
        {
            if (list.SelectedItem is not VersionChoice choice) throw new InvalidOperationException("Select a preserved version first.");
            string? destination = PickFolder("Choose a separate restore destination");
            if (destination is null) return;
            string restored = await vault.RestoreAsync(choice.Value.Id, destination);
            status.Text = "Verified copy restored: " + restored;
            MessageBox.Show(this, "Verified copy restored to:\n" + restored, "Recovery completed", MessageBoxButton.OK, MessageBoxImage.Information);
        }, true));
        page.Children.Add(Button("Refresh versions", async () => { await ReloadAsync(); status.Text = "Version list refreshed."; }));
        page.Children.Add(Text("No saved version means no guaranteed recovery. Local versions can be lost to same-user malware, an administrator, or disk failure; keep a separate backup."));
    }
    private void Activity()
    {
        page.Children.Add(Text("Latest 500 local events. Document paths and scanner output may contain private information."));
        var list = new ListBox { MaxHeight = 470, Padding = new Thickness(8) };
        foreach (var item in state.Activity) list.Items.Add(new TextBlock { Text = $"{item.At.LocalDateTime:g} · {item.Operation} · {item.Outcome}\n{item.Detail}", TextWrapping = TextWrapping.Wrap, Margin = new Thickness(6, 6, 6, 12) });
        page.Children.Add(list);
        page.Children.Add(Button("Refresh activity", async () => { await ReloadAsync(); status.Text = "Activity refreshed."; }));
    }
    private void Settings()
    {
        page.Children.Add(Text("Protected folders", 18, true));
        page.Children.Add(Text("Adding a folder captures an initial baseline and starts best-effort observation. Monitoring stops when this app closes."));
        page.Children.Add(Button("Add folder and capture baseline", async () =>
        {
            string? folder = PickFolder("Choose a folder to preserve"); if (folder is null) return;
            await vault.AddFolderAsync(folder);
            state = await vault.ReadAsync(); observer.Configure(state.ProtectedFolders);
            var summary = await Task.Run(() => vault.CaptureFolderAsync(folder));
            status.Text = $"Baseline: {summary.Saved} saved, {summary.Unchanged} unchanged, {summary.Failed} failed.";
        }, true));
        foreach (string folder in state.ProtectedFolders)
        {
            var content = new StackPanel(); content.Children.Add(Text(folder, 16, true));
            var actions = new WrapPanel();
            actions.Children.Add(Button("Capture baseline again", async () =>
            {
                var summary = await Task.Run(() => vault.CaptureFolderAsync(folder));
                status.Text = $"Baseline: {summary.Saved} saved, {summary.Unchanged} unchanged, {summary.Failed} failed.";
            }));
            actions.Children.Add(Button("Stop observing folder", async () =>
            {
                await vault.RemoveFolderAsync(folder); state = await vault.ReadAsync(); observer.Configure(state.ProtectedFolders);
                status.Text = "Folder removed; preserved versions retained.";
            }));
            content.Children.Add(actions); page.Children.Add(Panel(content));
        }
        page.Children.Add(Text("Recovery storage", 18, true));
        page.Children.Add(Text(vault.Root));
        page.Children.Add(Text($"Limits: {vault.Limits.MaxFileBytes / (1024 * 1024)} MiB per file · {vault.Limits.MaxVaultBytes / (1024 * 1024)} MiB vault · {vault.Limits.MaxVersions:N0} version records. Capacity exhaustion refuses new captures."));
        page.Children.Add(Text("The vault uses ordinary user permissions. It is not encrypted, immutable, or air-gapped. Company management and a background service are planned."));
    }
    public async Task SmokeAsync(string reportPath)
    {
        if (SmokeFailure is not null) throw new InvalidOperationException(SmokeFailure);
        string fixtures = vault.Root + "-fixtures";
        Directory.CreateDirectory(fixtures);
        string original = Path.Combine(fixtures, "simulation-report.txt");
        string restoreFolder = Path.Combine(fixtures, "restored");
        Directory.CreateDirectory(restoreFolder);
        await File.WriteAllTextAsync(original, "Benign original document for the desktop recovery simulation.");
        await vault.AddFolderAsync(fixtures);
        var baseline = await vault.CaptureFolderAsync(fixtures);
        var originalVersion = (await vault.ReadAsync()).Versions.Single(v => v.SourcePath == original);
        observer.Configure([fixtures]);
        await File.WriteAllTextAsync(original, "Simulated changed document.");
        await vault.CaptureAsync(original, "Desktop simulation");
        File.Delete(original);
        string copy = await vault.RestoreAsync(originalVersion.Id, restoreFolder);
        bool restored = await File.ReadAllTextAsync(copy) == "Benign original document for the desktop recovery simulation.";
        if (!restored || baseline.Saved != 1) throw new InvalidOperationException("Desktop recovery simulation failed.");
        await ReloadAsync(); var rendered = new List<string>();
        for (int i = 0; i < pages.Length; i++)
        {
            navigation.SelectedIndex = i; UpdateLayout();
            if (page.Children.Count < 2) throw new InvalidOperationException("Page failed to render: " + pages[i]);
            rendered.Add(pages[i]);
        }
        navigation.SelectedIndex = 0; UpdateLayout();
        Directory.CreateDirectory(Path.GetDirectoryName(reportPath)!);
        var visual = (FrameworkElement)Content;
        var bitmap = new RenderTargetBitmap((int)visual.ActualWidth, (int)visual.ActualHeight, 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(visual); var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using (var output = File.Create(Path.ChangeExtension(reportPath, ".png"))) encoder.Save(output);
        await File.WriteAllTextAsync(reportPath, JsonSerializer.Serialize(new { WindowOpened = IsLoaded,
            RenderedPages = rendered, ScannerFound = scanner.Executable is not null, BaselineCaptured = baseline.Saved,
            DeletedFileRestored = restored, Version = "0.1.0" }, new JsonSerializerOptions { WriteIndented = true }));
    }
}
