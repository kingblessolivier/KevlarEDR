using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using KevlarEDR.Core;
using static KevlarEDR.Desktop.DesktopTheme;

namespace KevlarEDR.Desktop;

public sealed class MainWindow : Window
{
    private readonly VersionVault vault;
    private readonly FolderObserver observer;
    private readonly DefenderScanner scanner = new();
    private readonly StackPanel page = new() { Margin = new Thickness(30, 26, 30, 20) };
    private readonly TextBlock status = new() { Margin = new Thickness(10, 0, 0, 0), TextWrapping = TextWrapping.Wrap, FontSize = 12, Foreground = Muted };
    private readonly ListBox navigation = new() { BorderThickness = new Thickness(0), Padding = new Thickness(0), Background = Brushes.Transparent };
    private VaultState state = new();
    private bool busy;
    private bool closing;
    private readonly string[] pages = ["Overview", "Scans", "Devices", "Recovery", "Activity", "Settings"];
    private readonly string[] pageIcons = [Computer, Search, Drive, Restore, History, DesktopTheme.Settings];
    private readonly string[] descriptions = ["Your computer, storage, and recovery at a glance.", "Scan files and folders with Microsoft Defender.",
        "Explore the storage connected to this computer.", "Find a saved version and restore a verified copy.", "Review captures, scans, and recovery operations.", "Manage observed folders and local version storage."];
    public string? SmokeFailure { get; set; }
    public MainWindow(string root)
    {
        vault = new(root); observer = new(vault);
        Title = "KevlarEDR — This computer";
        Width = 1240; Height = 860; MinWidth = 940; MinHeight = 640;
        FontFamily = new FontFamily("Segoe UI"); FontSize = 14;
        Foreground = Ink; Background = DesktopTheme.Color("#F3F6FB");
        Resources = DesktopTheme.Resources(); UseLayoutRounding = true; SnapsToDevicePixels = true;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        Icon = CreateWindowIcon();
        var shell = new DockPanel { Background = Background }; var rail = new DockPanel { Margin = new Thickness(18, 26, 18, 20) };
        var brand = new StackPanel { Margin = new Thickness(6, 0, 0, 30) };
        var brandRow = new StackPanel { Orientation = Orientation.Horizontal };
        brandRow.Children.Add(new Image { Source = Icon, Width = 32, Height = 32 });
        brandRow.Children.Add(new TextBlock { Text = "KevlarEDR", FontSize = 23, FontWeight = FontWeights.SemiBold, Foreground = Brushes.White, Margin = new Thickness(10, 0, 0, 0) });
        brand.Children.Add(brandRow); brand.Children.Add(Text("ENDPOINT WORKSPACE", 10, color: DesktopTheme.Color("#8599B5")));
        DockPanel.SetDock(brand, Dock.Top); rail.Children.Add(brand);
        var railFooter = new StackPanel { Margin = new Thickness(6, 20, 6, 0) };
        railFooter.Children.Add(new Border { Height = 1, Background = DesktopTheme.Color("#2A3C56"), Margin = new Thickness(0, 0, 0, 18) });
        railFooter.Children.Add(Text("LOCAL EDITION", 10, true, DesktopTheme.Color("#8599B5")));
        railFooter.Children.Add(Text("One computer", 14, true, Brushes.White));
        railFooter.Children.Add(Text("Company management is planned.", 12, color: DesktopTheme.Color("#9AACBF")));
        DockPanel.SetDock(railFooter, Dock.Bottom); rail.Children.Add(railFooter);
        var navigationArea = new StackPanel(); navigationArea.Children.Add(Text("WORKSPACE", 10, true, DesktopTheme.Color("#8599B5")));
        navigationArea.Children.Add(navigation); rail.Children.Add(navigationArea);
        var navBorder = new Border { Width = 228, Child = rail, Background = DesktopTheme.Color("#122238") };
        DockPanel.SetDock(navBorder, Dock.Left); shell.Children.Add(navBorder);
        for (int i = 0; i < pages.Length; i++)
        {
            var row = new StackPanel { Orientation = Orientation.Horizontal };
            var glyph = DesktopTheme.Icon(pageIcons[i], 20); glyph.ClearValue(ForegroundProperty); glyph.Margin = new Thickness(0, 0, 14, 0);
            row.Children.Add(glyph); row.Children.Add(new TextBlock { Text = pages[i], FontSize = 14, VerticalAlignment = VerticalAlignment.Center });
            var item = new ListBoxItem { Content = row, Style = (Style)Resources["NavigationItem"] };
            Accessible(item, pages[i]); navigation.Items.Add(item);
        }
        navigation.SelectionChanged += (_, _) => Draw();
        var workspace = new DockPanel();
        var top = new DockPanel { LastChildFill = true };
        var edition = Badge("Local preview  •  v0.1"); edition.VerticalAlignment = VerticalAlignment.Center; DockPanel.SetDock(edition, Dock.Right); top.Children.Add(edition);
        var computerLabel = new StackPanel { Orientation = Orientation.Horizontal };
        computerLabel.Children.Add(DesktopTheme.Icon(Computer, 20)); computerLabel.Children.Add(new TextBlock { Text = "This computer  /  " + Environment.MachineName,
            FontSize = 13, Foreground = Muted, Margin = new Thickness(10, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center });
        top.Children.Add(computerLabel);
        var header = new Border { Child = top, Background = Brushes.White, Padding = new Thickness(30, 16, 30, 16), BorderBrush = Line, BorderThickness = new Thickness(0, 0, 0, 1) };
        DockPanel.SetDock(header, Dock.Top); workspace.Children.Add(header);
        var statusLine = new Grid(); statusLine.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(16) }); statusLine.ColumnDefinitions.Add(new ColumnDefinition());
        statusLine.Children.Add(DesktopTheme.Icon(History, 16, Muted)); Grid.SetColumn(status, 1); statusLine.Children.Add(status);
        var footer = new Border { Child = statusLine, Background = Brushes.White, Padding = new Thickness(24, 10, 24, 10), BorderBrush = Line, BorderThickness = new Thickness(0, 1, 0, 0) };
        DockPanel.SetDock(footer, Dock.Bottom); workspace.Children.Add(footer);
        workspace.Children.Add(new ScrollViewer { Content = page, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled });
        shell.Children.Add(workspace);
        Content = shell; navigation.SelectedIndex = 0;
        observer.HealthChanged += message => Dispatcher.BeginInvoke(() => { if (!closing && !busy) status.Text = message; });
        Loaded += async (_, _) => await RunAsync(async () =>
        {
            await ReloadAsync(); observer.Configure(state.ProtectedFolders);
            status.Text = "Prototype: monitoring runs only while this app is open. " + observer.Health;
        });
        Closing += (_, _) => { closing = true; observer.DisposeAsync().AsTask().GetAwaiter().GetResult(); };
    }
    private static DrawingImage CreateWindowIcon()
    {
        var drawing = new DrawingGroup();
        using (var context = drawing.Open())
        {
            context.DrawRoundedRectangle(DesktopTheme.Color("#1769E0"), null, new Rect(0, 0, 48, 48), 10, 10);
            context.DrawGeometry(Brushes.White, null, Geometry.Parse("M24,8 L38,13 L37,26 Q34,36 24,41 Q14,36 11,26 L10,13 Z"));
            context.DrawGeometry(null, new Pen(DesktopTheme.Color("#1769E0"), 3), Geometry.Parse("M17,24 L22,29 L31,19"));
        }
        return new DrawingImage(drawing);
    }
    private Button Button(string label, Func<Task> operation, bool primary = false, string glyph = Chevron)
    {
        var content = new StackPanel { Orientation = Orientation.Horizontal };
        var icon = DesktopTheme.Icon(glyph, 16, primary ? Brushes.White : Blue); icon.Margin = new Thickness(0, 0, 9, 0);
        content.Children.Add(icon); content.Children.Add(new TextBlock { Text = label, VerticalAlignment = VerticalAlignment.Center, FontWeight = FontWeights.SemiBold, FontSize = 13 });
        var button = new Button { Content = content, Margin = new Thickness(0, 6, 10, 6),
            Background = primary ? Blue : Brushes.White, Foreground = primary ? Brushes.White : Ink,
            BorderBrush = primary ? Blue : Line, HorizontalAlignment = HorizontalAlignment.Left };
        Accessible(button, label);
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
        var heading = new DockPanel();
        var refresh = Button("Refresh", async () => { await ReloadAsync(); status.Text = observer.Health; }, glyph: Refresh);
        refresh.Margin = new Thickness(12, 0, 0, 0); refresh.VerticalAlignment = VerticalAlignment.Top; DockPanel.SetDock(refresh, Dock.Right); heading.Children.Add(refresh);
        var titles = new StackPanel(); titles.Children.Add(Text(selected == "Overview" ? "Computer overview" : selected, 28, true));
        titles.Children.Add(Text(descriptions[navigation.SelectedIndex], 13, color: Muted)); heading.Children.Add(titles); heading.Margin = new Thickness(0, 0, 0, 18); page.Children.Add(heading);
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
        var computer = new Grid(); computer.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(104) }); computer.ColumnDefinitions.Add(new ColumnDefinition());
        var computerIcon = IconTile(Computer, 64); computerIcon.Width = 88; computerIcon.Height = 88; computer.Children.Add(computerIcon);
        var details = new StackPanel(); details.Children.Add(Text("THIS COMPUTER", 10, true, Muted));
        details.Children.Add(Text(Environment.MachineName, 24, true));
        details.Children.Add(Text($"Windows {(Environment.OSVersion.Version.Build >= 22000 ? "11" : "10")}  •  Build {Environment.OSVersion.Version.Build}  •  {System.Runtime.InteropServices.RuntimeInformation.OSArchitecture}", 13, color: Muted));
        details.Children.Add(Badge(state.ProtectedFolders.Count == 0 ? "Choose folders to start preserving versions" : "Local folder observation configured", state.ProtectedFolders.Count == 0));
        Grid.SetColumn(details, 1); computer.Children.Add(details); page.Children.Add(Card(computer, 24));
        var drives = DriveInfo.GetDrives(); var metrics = new Grid();
        for (int i = 0; i < 3; i++) metrics.ColumnDefinitions.Add(new ColumnDefinition());
        AddMetric(metrics, 0, Folder, state.ProtectedFolders.Count.ToString(), "Observed folders", "Selected in Settings");
        AddMetric(metrics, 1, Restore, state.Versions.Count.ToString("N0"), "Saved versions", "Available for verified restore");
        AddMetric(metrics, 2, Drive, drives.Length.ToString(), "Connected drives", "Reported by Windows");
        page.Children.Add(metrics);
        var middle = new Grid(); middle.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.25, GridUnitType.Star) }); middle.ColumnDefinitions.Add(new ColumnDefinition());
        var health = new StackPanel(); health.Children.Add(Text("Protection components", 17, true));
        health.Children.Add(ComponentRow(Folder, "Folder observation", observer.Health));
        health.Children.Add(ComponentRow(Shield, "Microsoft Defender", scanner.Executable is null ? "Scanner unavailable" : "Scanner found • on-demand scans available"));
        health.Children.Add(ComponentRow(Restore, "Local recovery", "SHA-256 verification • restore as a separate copy"));
        var healthCard = Card(health); healthCard.Margin = new Thickness(0, 0, 16, 16); middle.Children.Add(healthCard);
        var actions = new StackPanel(); actions.Children.Add(Text("Quick actions", 17, true));
        actions.Children.Add(Button("Scan files or a folder", () => NavigateAsync(1), true, Search));
        actions.Children.Add(Button("Add an observed folder", () => NavigateAsync(5), glyph: Add));
        actions.Children.Add(Button("Recover a saved version", () => NavigateAsync(3), glyph: Restore));
        var actionsCard = Card(actions); Grid.SetColumn(actionsCard, 1); middle.Children.Add(actionsCard); page.Children.Add(middle);
        Notice("Observation runs while this app is open. Versions are captured after changes; write blocking and malware containment are planned.");
        var recent = new StackPanel(); recent.Children.Add(Text("Recent activity", 17, true));
        foreach (var item in state.Activity.Take(3)) recent.Children.Add(EventRow(item));
        if (state.Activity.Count == 0) recent.Children.Add(Text("No activity yet. Add a folder or request your first scan.", 13, color: Muted));
        page.Children.Add(Card(recent));
    }
    private static void AddMetric(Grid grid, int column, string glyph, string value, string label, string detail)
    {
        var layout = new DockPanel(); var tile = IconTile(glyph); tile.Margin = new Thickness(0, 0, 16, 0); DockPanel.SetDock(tile, Dock.Left); layout.Children.Add(tile);
        var labels = new StackPanel(); labels.Children.Add(Text(value, 27, true)); labels.Children.Add(Text(label, 13, true)); labels.Children.Add(Text(detail, 11, color: Muted)); layout.Children.Add(labels);
        var card = Card(layout, 18); card.Margin = new Thickness(0, 0, column < 2 ? 16 : 0, 16); Grid.SetColumn(card, column); grid.Children.Add(card);
    }
    private static UIElement ComponentRow(string glyph, string title, string value)
    {
        var row = new DockPanel { Margin = new Thickness(0, 12, 0, 0) }; var icon = DesktopTheme.Icon(glyph, 20); icon.Margin = new Thickness(0, 0, 14, 8); DockPanel.SetDock(icon, Dock.Left); row.Children.Add(icon);
        var labels = new StackPanel(); labels.Children.Add(Text(title, 13, true)); labels.Children.Add(Text(value, 12, color: Muted)); row.Children.Add(labels); return row;
    }
    private void Notice(string message)
    {
        var row = new DockPanel(); var icon = DesktopTheme.Icon(Warning, 20, DesktopTheme.Color("#946211")); icon.Margin = new Thickness(0, 0, 12, 0); DockPanel.SetDock(icon, Dock.Left); row.Children.Add(icon);
        row.Children.Add(new TextBlock { Text = message, TextWrapping = TextWrapping.Wrap, FontSize = 12, Foreground = DesktopTheme.Color("#78521C") });
        page.Children.Add(new Border { Child = row, Background = DesktopTheme.Color("#FFF5E6"), BorderBrush = DesktopTheme.Color("#F3DEBD"), BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(9), Padding = new Thickness(16), Margin = new Thickness(0, 0, 0, 16) });
    }
    private static UIElement EventRow(ActivityEntry item)
    {
        var row = new DockPanel { Margin = new Thickness(0, 10, 0, 0) };
        var time = Text(item.At.LocalDateTime.ToString("HH:mm"), 12, color: Muted); time.Width = 48; DockPanel.SetDock(time, Dock.Left); row.Children.Add(time);
        var labels = new StackPanel(); labels.Children.Add(Text(item.Operation + "  •  " + item.Outcome, 13, true));
        labels.Children.Add(new TextBlock { Text = item.Detail, FontSize = 12, Foreground = Muted, TextTrimming = TextTrimming.CharacterEllipsis, ToolTip = item.Detail }); row.Children.Add(labels); return row;
    }
    private static string? PickFolder(string title)
    {
        var dialog = new OpenFolderDialog { Title = title, Multiselect = false };
        return dialog.ShowDialog() == true ? dialog.FolderName : null;
    }
    private void Scans()
    {
        var scannerPanel = new DockPanel(); var shield = IconTile(Shield, 48); shield.Margin = new Thickness(0, 0, 20, 0); DockPanel.SetDock(shield, Dock.Left); scannerPanel.Children.Add(shield);
        var provider = Label(Shield, "Microsoft Defender", "Custom scans of the files, folders, or drives you select.");
        provider.Children.Add(Badge(scanner.Executable is null ? "Scanner unavailable" : "Scanner found", scanner.Executable is null)); scannerPanel.Children.Add(provider); page.Children.Add(Card(scannerPanel, 24));
        var choices = new WrapPanel();
        choices.Children.Add(Button("Choose folder and scan", async () => { string? folder = PickFolder("Choose a folder to scan"); if (folder is not null) await ScanAsync(folder); }, true, Folder));
        choices.Children.Add(Button("Choose file and scan", async () =>
        {
            var dialog = new OpenFileDialog { Title = "Choose a file to scan", CheckFileExists = true };
            if (dialog.ShowDialog(this) == true) await ScanAsync(dialog.FileName);
        }, glyph: Document)); choices.Children.Add(Button("Browse drives", () => NavigateAsync(2), glyph: Drive));
        page.Children.Add(choices); page.Children.Add(new Border { Height = 16 });
        var results = new StackPanel(); results.Children.Add(Text("Last scan result", 18, true));
        if (state.LastScan is { } scan)
        {
            results.Children.Add(Badge(scan.Status, scan.ExitCode != 0));
            results.Children.Add(Text($"Target: {state.LastScanTarget}\nExit code: {scan.ExitCode?.ToString() ?? "Unavailable"}  •  {scan.CompletedAt.LocalDateTime:g}", 13, color: Muted));
            var output = new TextBox { Text = scan.Output, IsReadOnly = true, TextWrapping = TextWrapping.Wrap,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto, MaxHeight = 280, MinHeight = 100, FontFamily = new FontFamily("Cascadia Mono, Consolas"), FontSize = 12 };
            Accessible(output, "Defender scan output"); results.Children.Add(output);
        }
        else results.Children.Add(Label(Search, "Ready for your first scan", "Select a file or folder above. The provider result will appear here."));
        page.Children.Add(Card(results));
        Notice("Automatic remediation is disabled for this scan request. Review provider output and errors; completion alone does not establish safety.");
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
        var computer = Label(Computer, Environment.MachineName, "Storage inventory • local computer"); page.Children.Add(Card(computer));
        foreach (var drive in DriveInfo.GetDrives())
        {
            var layout = new DockPanel(); var tile = IconTile(drive.DriveType == DriveType.Removable ? Usb : Drive, 40); tile.Margin = new Thickness(0, 0, 22, 0); DockPanel.SetDock(tile, Dock.Left); layout.Children.Add(tile);
            var content = new StackPanel(); bool ready = false;
            try
            {
                ready = drive.IsReady;
                content.Children.Add(Text((ready && !string.IsNullOrWhiteSpace(drive.VolumeLabel) ? drive.VolumeLabel : drive.DriveType == DriveType.Removable ? "Removable drive" : "Local drive") + " (" + drive.Name.TrimEnd('\\') + ")", 18, true));
                if (ready)
                {
                    content.Children.Add(Text($"{drive.DriveType}  •  {drive.DriveFormat}  •  {drive.AvailableFreeSpace / (1024d * 1024 * 1024):F1} GiB free of {drive.TotalSize / (1024d * 1024 * 1024):F1} GiB", 12, color: Muted));
                    var space = new ProgressBar { Value = drive.TotalSize > 0 ? 100d * (drive.TotalSize - drive.TotalFreeSpace) / drive.TotalSize : 0,
                        Maximum = 100, Height = 6, Foreground = Blue, Background = DesktopTheme.Color("#E7EDF6"), BorderThickness = new Thickness(0), Margin = new Thickness(0, 5, 0, 10) };
                    Accessible(space, "Used capacity on " + drive.Name); content.Children.Add(space);
                }
                else content.Children.Add(Text("Not ready • insert media to access this drive", 13, color: Muted));
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                ready = false; content.Children.Add(Text(drive.Name + " • information unavailable", 14, true));
            }
            if (ready) { string path = drive.RootDirectory.FullName; content.Children.Add(Button("Scan this drive", () => ScanAsync(path), glyph: Search)); }
            layout.Children.Add(content); page.Children.Add(Card(layout, 24));
        }
        Notice("Drive type does not establish trust; external disks can appear as Fixed. USB blocking, device approval, and read-only enforcement are planned.");
    }
    private sealed record VersionChoice(FileVersion Value)
    {
        public override string ToString() => $"{Value.FileName} · {Value.CapturedAt.LocalDateTime:g} · {Value.Size:N0} bytes\n{Value.SourcePath}";
    }
    private void Recovery()
    {
        page.Children.Add(Card(Label(Restore, "Verified file recovery", "Choose a saved version. Each restore checks its SHA-256 and creates a separate copy.")));
        var workspace = new StackPanel(); var title = new DockPanel(); var count = Badge(state.Versions.Count.ToString("N0") + " versions"); DockPanel.SetDock(count, Dock.Right); title.Children.Add(count); title.Children.Add(Text("Version library", 17, true)); workspace.Children.Add(title);
        workspace.Children.Add(Text("Search by file name or original path", 12, color: Muted));
        var filter = new TextBox { Margin = new Thickness(0, 0, 0, 12) }; Accessible(filter, "Search saved versions"); workspace.Children.Add(filter);
        var list = new ListBox { MinHeight = 150, MaxHeight = 310, Padding = new Thickness(4), Margin = new Thickness(0, 0, 0, 10) };
        Accessible(list, "Saved file versions");
        void Populate()
        {
            list.Items.Clear();
            foreach (var version in state.Versions.OrderByDescending(v => v.CapturedAt).Where(v => v.SourcePath.Contains(filter.Text, StringComparison.OrdinalIgnoreCase)))
            {
                var row = new DockPanel(); var icon = DesktopTheme.Icon(Document, 24); icon.Margin = new Thickness(0, 0, 14, 0); DockPanel.SetDock(icon, Dock.Left); row.Children.Add(icon);
                var labels = new StackPanel(); labels.Children.Add(Text(version.FileName, 14, true));
                labels.Children.Add(Text($"{version.CapturedAt.LocalDateTime:g}  •  {version.Size:N0} bytes", 12, color: Muted));
                labels.Children.Add(new TextBlock { Text = version.SourcePath, Foreground = Muted, FontSize = 11, TextTrimming = TextTrimming.CharacterEllipsis, ToolTip = version.SourcePath }); row.Children.Add(labels);
                var item = new ListBoxItem { Content = row, Tag = new VersionChoice(version) }; Accessible(item, new VersionChoice(version).ToString()); list.Items.Add(item);
            }
        }
        filter.TextChanged += (_, _) => Populate(); Populate();
        workspace.Children.Add(list);
        if (state.Versions.Count == 0) workspace.Children.Add(Text("No saved versions. Add a folder in Settings to capture a baseline.", 13, color: Muted));
        workspace.Children.Add(Button("Restore selected version as a copy", async () =>
        {
            if (list.SelectedItem is not ListBoxItem { Tag: VersionChoice choice }) throw new InvalidOperationException("Select a preserved version first.");
            string? destination = PickFolder("Choose a separate restore destination");
            if (destination is null) return;
            string restored = await vault.RestoreAsync(choice.Value.Id, destination);
            status.Text = "Verified copy restored: " + restored;
            MessageBox.Show(this, "Verified copy restored to:\n" + restored, "Recovery completed", MessageBoxButton.OK, MessageBoxImage.Information);
        }, true, Restore)); page.Children.Add(Card(workspace));
        Notice("Recovery requires a saved version. Local storage can be lost to malware, an administrator, or disk failure; keep a separate backup.");
    }
    private void Activity()
    {
        var events = new StackPanel(); events.Children.Add(Label(History, "Local event history", "Latest 500 events. File paths and scanner output can contain private information."));
        var filter = new TextBox { Margin = new Thickness(0, 4, 0, 12) }; Accessible(filter, "Search activity"); events.Children.Add(Text("Search operations, outcomes, or details", 12, color: Muted)); events.Children.Add(filter);
        var list = new ListBox { MaxHeight = 460, MinHeight = 150, Padding = new Thickness(4) }; Accessible(list, "Local activity events");
        void Populate()
        {
            list.Items.Clear();
            foreach (var item in state.Activity.Where(a => (a.Operation + a.Outcome + a.Detail).Contains(filter.Text, StringComparison.OrdinalIgnoreCase)))
            {
                var content = new StackPanel(); content.Children.Add(Text($"{item.At.LocalDateTime:g}  •  {item.Operation}  •  {item.Outcome}", 13, true)); content.Children.Add(Text(item.Detail, 12, color: Muted)); list.Items.Add(content);
            }
        }
        filter.TextChanged += (_, _) => Populate(); Populate(); events.Children.Add(list);
        if (state.Activity.Count == 0) events.Children.Add(Text("No events recorded yet.", 13, color: Muted)); page.Children.Add(Card(events));
    }
    private void Settings()
    {
        page.Children.Add(Card(Label(Folder, "Observed folders", "Capture an initial baseline, then preserve readable versions after observed changes. Monitoring stops when this app closes.")));
        page.Children.Add(Button("Add folder and capture baseline", async () =>
        {
            string? folder = PickFolder("Choose a folder to preserve"); if (folder is null) return;
            await vault.AddFolderAsync(folder);
            state = await vault.ReadAsync(); observer.Configure(state.ProtectedFolders);
            var summary = await Task.Run(() => vault.CaptureFolderAsync(folder));
            status.Text = $"Baseline: {summary.Saved} saved, {summary.Unchanged} unchanged, {summary.Failed} failed.";
        }, true, Add));
        foreach (string folder in state.ProtectedFolders)
        {
            var content = Label(Folder, Path.GetFileName(folder), folder);
            var actions = new WrapPanel();
            actions.Children.Add(Button("Capture baseline again", async () =>
            {
                var summary = await Task.Run(() => vault.CaptureFolderAsync(folder));
                status.Text = $"Baseline: {summary.Saved} saved, {summary.Unchanged} unchanged, {summary.Failed} failed.";
            }, glyph: Refresh));
            actions.Children.Add(Button("Stop observing folder", async () =>
            {
                await vault.RemoveFolderAsync(folder); state = await vault.ReadAsync(); observer.Configure(state.ProtectedFolders);
                status.Text = "Folder removed; preserved versions retained.";
            }));
            content.Children.Add(actions); page.Children.Add(Card(content));
        }
        var storage = Label(Drive, "Recovery storage", vault.Root);
        storage.Children.Add(Text($"{vault.Limits.MaxFileBytes / (1024 * 1024)} MiB per file  •  {vault.Limits.MaxVaultBytes / (1024 * 1024)} MiB vault  •  {vault.Limits.MaxVersions:N0} version records", 13, true));
        storage.Children.Add(Text("When capacity is exhausted, new captures fail visibly. Existing versions are retained.", 12, color: Muted)); page.Children.Add(Card(storage));
        Notice("The vault uses ordinary user permissions. Encryption, tamper-resistant storage, a background service, and company management are planned.");
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
        Directory.CreateDirectory(Path.GetDirectoryName(reportPath)!);
        var typeface = new Typeface(new FontFamily("Segoe Fluent Icons"), FontStyles.Normal, FontWeights.Normal, FontStretches.Normal);
        if (!typeface.TryGetGlyphTypeface(out var glyphTypeface) || DesktopTheme.Glyphs.Any(g => !glyphTypeface.CharacterToGlyphMap.ContainsKey(g[0])))
            throw new InvalidOperationException("A required Windows icon is missing from the installed font.");
        for (int i = 0; i < pages.Length; i++)
        {
            navigation.SelectedIndex = i; UpdateLayout();
            if (page.Children.Count < 2) throw new InvalidOperationException("Page failed to render: " + pages[i]);
            SaveScreenshot(Path.Combine(Path.GetDirectoryName(reportPath)!, Path.GetFileNameWithoutExtension(reportPath) + "-" + pages[i].ToLowerInvariant() + ".png"));
            rendered.Add(pages[i]);
        }
        navigation.SelectedIndex = 3; UpdateLayout();
        var recoveryFilter = Descendants<TextBox>(page).Single(t => System.Windows.Automation.AutomationProperties.GetName(t) == "Search saved versions");
        var versionList = Descendants<ListBox>(page).Single();
        recoveryFilter.Text = "definitely-not-a-saved-file";
        if (versionList.Items.Count != 0) throw new InvalidOperationException("Recovery search did not filter versions.");
        recoveryFilter.Text = "simulation-report";
        if (versionList.Items.Count < 2) throw new InvalidOperationException("Recovery search did not find saved versions.");
        versionList.SelectedIndex = 0;
        if (versionList.SelectedItem is not ListBoxItem { Tag: VersionChoice }) throw new InvalidOperationException("Recovery selection lost its version identity.");
        navigation.SelectedIndex = 4; UpdateLayout();
        var activityFilter = Descendants<TextBox>(page).Single();
        activityFilter.Text = "definitely-not-an-event";
        if (Descendants<ListBox>(page).Single().Items.Count != 0) throw new InvalidOperationException("Activity search did not filter events.");
        navigation.SelectedIndex = 0; UpdateLayout();
        SaveScreenshot(Path.ChangeExtension(reportPath, ".png"));
        Width = MinWidth; Height = MinHeight; UpdateLayout();
        SaveScreenshot(Path.Combine(Path.GetDirectoryName(reportPath)!, Path.GetFileNameWithoutExtension(reportPath) + "-compact.png"));
        await File.WriteAllTextAsync(reportPath, JsonSerializer.Serialize(new { WindowOpened = IsLoaded,
            RenderedPages = rendered, ScannerFound = scanner.Executable is not null, BaselineCaptured = baseline.Saved,
            DeletedFileRestored = restored, NativeIconGlyphsVerified = DesktopTheme.Glyphs.Length,
            RecoverySearchPassed = true, ActivitySearchPassed = true, CompactLayoutRendered = true,
            Version = "0.1.0" }, new JsonSerializerOptions { WriteIndented = true }));
    }
    private static IEnumerable<T> Descendants<T>(DependencyObject parent) where T : DependencyObject
    {
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            if (child is T match) yield return match;
            foreach (var descendant in Descendants<T>(child)) yield return descendant;
        }
    }
    private void SaveScreenshot(string path)
    {
        var visual = (FrameworkElement)Content;
        var bitmap = new RenderTargetBitmap((int)visual.ActualWidth, (int)visual.ActualHeight, 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(visual); var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using var output = File.Create(path); encoder.Save(output);
    }
}
