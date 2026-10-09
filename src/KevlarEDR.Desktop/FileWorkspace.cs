using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using KevlarEDR.Core;
using static KevlarEDR.Desktop.DesktopTheme;

namespace KevlarEDR.Desktop;

internal sealed record BrowserItem(FileEntry Entry, ImageSource Icon)
{
    public string Name => Entry.Name;
    public string Modified => Entry.LastModified.ToString("g");
    public string Kind => Entry.IsLink ? "Linked " + (Entry.IsDirectory ? "folder" : "file") : Entry.IsDirectory ? "File folder" :
        string.IsNullOrEmpty(Path.GetExtension(Name)) ? "File" : Path.GetExtension(Name).TrimStart('.').ToUpperInvariant() + " file";
    public string Size => Entry.Size is null ? "" : FormatBytes(Entry.Size.Value);
    internal static string FormatBytes(long bytes) => bytes < 1024 ? $"{bytes:N0} bytes" : bytes < 1024 * 1024 ? $"{bytes / 1024d:N1} KB" :
        bytes < 1024L * 1024 * 1024 ? $"{bytes / (1024d * 1024):N1} MB" : $"{bytes / (1024d * 1024 * 1024):N1} GB";
}

internal sealed class FileWorkspace : UserControl, IDisposable
{
    private readonly LocalFileBrowser browser = new();
    private readonly Func<string, Task> scan, preserve, recovery;
    private readonly Stack<string?> back = new(), forward = new();
    private readonly TextBox address = new(), search = new();
    private readonly TextBlock summary = new() { FontSize = 12, Foreground = Muted, TextWrapping = TextWrapping.Wrap };
    private readonly TextBlock message = new() { FontSize = 12, Foreground = Muted, TextWrapping = TextWrapping.Wrap };
    private readonly ContentControl listing = new();
    private readonly StackPanel details = new() { Margin = new Thickness(18) };
    private readonly Grid body = new();
    private readonly Button backButton, forwardButton, upButton, scanButton, preserveButton, recoveryButton;
    private readonly CheckBox hidden = new() { Content = "Hidden items", VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(10, 0, 14, 0) };
    private readonly ComboBox view = new() { Width = 98, Margin = new Thickness(10, 0, 0, 0), Padding = new Thickness(6), VerticalAlignment = VerticalAlignment.Center };
    private readonly DataGrid table = new() { AutoGenerateColumns = false, IsReadOnly = true, CanUserAddRows = false,
        CanUserDeleteRows = false, CanUserReorderColumns = true, SelectionMode = DataGridSelectionMode.Single,
        SelectionUnit = DataGridSelectionUnit.FullRow, RowHeight = 35, HeadersVisibility = DataGridHeadersVisibility.Column,
        GridLinesVisibility = DataGridGridLinesVisibility.None, BorderThickness = new Thickness(0), Background = Brushes.White,
        RowBackground = Brushes.White, AlternatingRowBackground = Brushes.White, HorizontalScrollBarVisibility = ScrollBarVisibility.Auto };
    private readonly ListBox tiles = new() { BorderThickness = new Thickness(0), Background = Brushes.White, Padding = new Thickness(12) };
    private CancellationTokenSource? load;
    private int request;
    private bool loading, disposed;
    private List<BrowserItem> items = [];
    private BrowserItem? selected;
    private DirectorySnapshot? snapshot;
    public string? CurrentLocation { get; private set; }
    public int VisibleItems => table.Items.Count;
    public bool HasShellIcons => items.Count > 0 && items.All(i => i.Icon is not null);
    public bool CanScanSelection => scanButton.IsEnabled;
    public event Action<string?>? LocationChanged;
    public static IReadOnlyList<(string Label, string Path)> PersonalFolders()
    {
        var profile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        return new (string Label, string Path)[] {
            ("Desktop", Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory)),
            ("Documents", Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)),
            ("Downloads", Path.Combine(profile, "Downloads")),
            ("Pictures", Environment.GetFolderPath(Environment.SpecialFolder.MyPictures)),
            ("Music", Environment.GetFolderPath(Environment.SpecialFolder.MyMusic)),
            ("Videos", Environment.GetFolderPath(Environment.SpecialFolder.MyVideos)) }.Where(p => !string.IsNullOrEmpty(p.Path)).ToArray();
    }
    public FileWorkspace(Func<string, Task> scan, Func<string, Task> preserve, Func<string, Task> recovery)
    {
        this.scan = scan; this.preserve = preserve; this.recovery = recovery;
        Background = Brushes.White; FontSize = 13;
        var shell = new DockPanel();
        var locations = new Grid { Margin = new Thickness(16, 14, 16, 12) };
        locations.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); locations.ColumnDefinitions.Add(new ColumnDefinition());
        locations.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(212) });
        var travel = new StackPanel { Orientation = Orientation.Horizontal };
        backButton = Tool("Back", "\uE72B", async () => { await BackAsync(); }, true);
        forwardButton = Tool("Forward", "\uE72A", async () => { await ForwardAsync(); }, true);
        upButton = Tool("Up one folder", "\uE74A", async () => { await UpAsync(); }, true);
        travel.Children.Add(backButton); travel.Children.Add(forwardButton); travel.Children.Add(upButton);
        travel.Children.Add(Tool("Refresh folder", Refresh, async () => { await NavigateAsync(CurrentLocation, false); }, true)); locations.Children.Add(travel);
        address.Padding = new Thickness(12, 9, 12, 9); address.VerticalContentAlignment = VerticalAlignment.Center;
        Accessible(address, "Folder location"); address.KeyDown += async (_, e) => { if (e.Key == Key.Enter) { e.Handled = true; await NavigateAsync(address.Text.Equals("This PC", StringComparison.OrdinalIgnoreCase) ? null : address.Text); } };
        var locationPanel = new DockPanel { Margin = new Thickness(10, 0, 12, 0) };
        var locationIcon = DesktopTheme.Icon(Computer, 20); locationIcon.Margin = new Thickness(0, 0, 9, 0); DockPanel.SetDock(locationIcon, Dock.Left); locationPanel.Children.Add(locationIcon); locationPanel.Children.Add(address);
        Grid.SetColumn(locationPanel, 1); locations.Children.Add(locationPanel);
        var searchPanel = new DockPanel(); var searchIcon = DesktopTheme.Icon(Search, 16, Muted); searchIcon.Margin = new Thickness(0, 0, 8, 0); DockPanel.SetDock(searchIcon, Dock.Left); searchPanel.Children.Add(searchIcon);
        search.ToolTip = "Filter names in the current folder"; Accessible(search, "Search current folder"); search.TextChanged += (_, _) => Filter();
        var searchField = new Grid(); searchField.Children.Add(search);
        var hint = new TextBlock { Text = "Search this folder", FontSize = 12, Foreground = Muted, Margin = new Thickness(12, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center, IsHitTestVisible = false };
        searchField.Children.Add(hint); search.TextChanged += (_, _) => hint.Visibility = string.IsNullOrEmpty(search.Text) ? Visibility.Visible : Visibility.Collapsed;
        searchPanel.Children.Add(searchField);
        Grid.SetColumn(searchPanel, 2); locations.Children.Add(searchPanel); DockPanel.SetDock(locations, Dock.Top); shell.Children.Add(locations);
        var commands = new DockPanel { Margin = new Thickness(16, 0, 16, 0) };
        view.Items.Add("Details"); view.Items.Add("Icons"); view.SelectedIndex = 0;
        view.SelectionChanged += (_, _) => { if (CurrentLocation is not null) ShowFiles(); }; Accessible(view, "File view"); DockPanel.SetDock(view, Dock.Right); commands.Children.Add(view);
        hidden.Checked += async (_, _) => { await NavigateAsync(CurrentLocation, false); }; hidden.Unchecked += async (_, _) => { await NavigateAsync(CurrentLocation, false); };
        DockPanel.SetDock(hidden, Dock.Right); commands.Children.Add(hidden);
        var actions = new WrapPanel(); scanButton = Tool("Scan selected", Search, ScanSelectedAsync); preserveButton = Tool("Preserve folder", Shield, PreserveSelectedAsync);
        recoveryButton = Tool("Saved versions", Restore, RecoverSelectedAsync); actions.Children.Add(scanButton); actions.Children.Add(preserveButton); actions.Children.Add(recoveryButton); commands.Children.Add(actions);
        var commandBorder = new Border { Child = commands, Background = DesktopTheme.Color("#FAFAFA"), BorderBrush = Line, BorderThickness = new Thickness(0, 1, 0, 1), Padding = new Thickness(0, 6, 0, 6) };
        DockPanel.SetDock(commandBorder, Dock.Top); shell.Children.Add(commandBorder);
        var progress = new Border { Child = message, Padding = new Thickness(20, 8, 20, 8), Background = DesktopTheme.Color("#FAFCFF") };
        DockPanel.SetDock(progress, Dock.Top); shell.Children.Add(progress);
        var footer = new Border { Child = summary, Padding = new Thickness(18, 10, 18, 10), Background = DesktopTheme.Color("#FAFAFA"), BorderBrush = Line, BorderThickness = new Thickness(0, 1, 0, 0) };
        DockPanel.SetDock(footer, Dock.Bottom); shell.Children.Add(footer);
        body.ColumnDefinitions.Add(new ColumnDefinition()); body.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(224) });
        body.Children.Add(listing); var detailBorder = new Border { Child = details, Background = DesktopTheme.Color("#FBFCFE"), BorderBrush = Line, BorderThickness = new Thickness(1, 0, 0, 0) };
        Grid.SetColumn(detailBorder, 1); body.Children.Add(detailBorder); shell.Children.Add(body); Content = shell;
        SizeChanged += (_, _) => body.ColumnDefinitions[1].Width = new GridLength(ActualWidth < 850 ? 0 : 224);
        var nameTemplate = new DataTemplate { VisualTree = FileNameFactory() };
        table.Columns.Add(new DataGridTemplateColumn { Header = "Name", CellTemplate = nameTemplate, SortMemberPath = "Name", Width = new DataGridLength(1, DataGridLengthUnitType.Star), MinWidth = 170 });
        table.Columns.Add(new DataGridTextColumn { Header = "Date modified", Binding = new Binding("Modified"), SortMemberPath = "Entry.LastModified", Width = 150 });
        table.Columns.Add(new DataGridTextColumn { Header = "Type", Binding = new Binding("Kind"), Width = 115 });
        table.Columns.Add(new DataGridTextColumn { Header = "Size", Binding = new Binding("Size"), SortMemberPath = "Entry.Size", Width = 95 });
        Accessible(table, "Files and folders"); table.SelectionChanged += (_, _) => Select(table.SelectedItem as BrowserItem);
        table.MouseDoubleClick += async (_, e) => { if (e.OriginalSource is DependencyObject source && FindParent<DataGridRow>(source) is { Item: BrowserItem item }) await OpenAsync(item); };
        table.KeyDown += async (_, e) => { if (e.Key == Key.Enter && selected is not null) { e.Handled = true; await OpenAsync(selected); } };
        tiles.ItemsPanel = new ItemsPanelTemplate(new FrameworkElementFactory(typeof(WrapPanel)));
        var tileStack = new FrameworkElementFactory(typeof(StackPanel)); tileStack.SetValue(WidthProperty, 128d); tileStack.SetValue(MarginProperty, new Thickness(4, 8, 4, 8));
        var tileIcon = new FrameworkElementFactory(typeof(Image)); tileIcon.SetBinding(Image.SourceProperty, new Binding("Icon")); tileIcon.SetValue(WidthProperty, 32d); tileIcon.SetValue(HeightProperty, 32d); tileIcon.SetValue(MarginProperty, new Thickness(0, 0, 0, 9)); tileStack.AppendChild(tileIcon);
        var tileName = new FrameworkElementFactory(typeof(TextBlock)); tileName.SetBinding(TextBlock.TextProperty, new Binding("Name")); tileName.SetValue(TextBlock.TextAlignmentProperty, TextAlignment.Center); tileName.SetValue(TextBlock.TextTrimmingProperty, TextTrimming.CharacterEllipsis); tileStack.AppendChild(tileName);
        tiles.ItemTemplate = new DataTemplate { VisualTree = tileStack }; Accessible(tiles, "File icons");
        tiles.SelectionChanged += (_, _) => Select(tiles.SelectedItem as BrowserItem);
        tiles.MouseDoubleClick += async (_, e) => { if (e.OriginalSource is DependencyObject source && FindParent<ListBoxItem>(source)?.DataContext is BrowserItem item) await OpenAsync(item); };
        tiles.KeyDown += async (_, e) => { if (e.Key == Key.Enter && selected is not null) { e.Handled = true; await OpenAsync(selected); } };
        AddContextMenu(table); AddContextMenu(tiles);
        PreviewKeyDown += async (_, e) =>
        {
            if (Keyboard.Modifiers == ModifierKeys.Alt && e.Key == Key.Left) { e.Handled = true; await BackAsync(); }
            else if (Keyboard.Modifiers == ModifierKeys.Alt && e.Key == Key.Right) { e.Handled = true; await ForwardAsync(); }
            else if (Keyboard.Modifiers == ModifierKeys.Alt && e.Key == Key.Up) { e.Handled = true; await UpAsync(); }
            else if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.L) { e.Handled = true; address.Focus(); address.SelectAll(); }
            else if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.F) { e.Handled = true; search.Focus(); search.SelectAll(); }
        };
        UpdateButtons();
    }
    private static FrameworkElementFactory FileNameFactory()
    {
        var row = new FrameworkElementFactory(typeof(StackPanel)); row.SetValue(StackPanel.OrientationProperty, Orientation.Horizontal); row.SetValue(MarginProperty, new Thickness(10, 0, 0, 0)); row.SetValue(VerticalAlignmentProperty, VerticalAlignment.Center);
        var image = new FrameworkElementFactory(typeof(Image)); image.SetBinding(Image.SourceProperty, new Binding("Icon")); image.SetValue(WidthProperty, 20d); image.SetValue(HeightProperty, 20d); image.SetValue(MarginProperty, new Thickness(0, 0, 10, 0)); row.AppendChild(image);
        var name = new FrameworkElementFactory(typeof(TextBlock)); name.SetBinding(TextBlock.TextProperty, new Binding("Name")); name.SetValue(VerticalAlignmentProperty, VerticalAlignment.Center); row.AppendChild(name); return row;
    }
    private Button Tool(string label, string glyph, Func<Task> action, bool iconOnly = false)
    {
        var content = new StackPanel { Orientation = Orientation.Horizontal }; content.Children.Add(DesktopTheme.Icon(glyph, 16, Muted));
        if (!iconOnly) content.Children.Add(new TextBlock { Text = label, Margin = new Thickness(8, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center, FontSize = 12 });
        var button = new Button { Content = content, Padding = new Thickness(iconOnly ? 9 : 10, 7, iconOnly ? 9 : 10, 7), MinHeight = 34, Margin = new Thickness(0, 0, 5, 0), Background = Brushes.Transparent, BorderBrush = Brushes.Transparent, ToolTip = label };
        Accessible(button, label); button.Click += async (_, _) => await ActionAsync(action); return button;
    }
    private async Task ActionAsync(Func<Task> action)
    {
        try { await action(); }
        catch (Exception ex) { message.Text = ex.Message; message.Foreground = DesktopTheme.Color("#A42A2A"); }
    }
    private void AddContextMenu(Control control)
    {
        var menu = new ContextMenu();
        foreach (var (label, action) in new (string, Func<Task>)[] { ("Open folder", () => selected is null ? Task.CompletedTask : OpenAsync(selected)), ("Scan with Defender", ScanSelectedAsync), ("Preserve this folder", PreserveSelectedAsync), ("View saved versions", RecoverSelectedAsync) })
        { var item = new MenuItem { Header = label }; item.Click += async (_, _) => await ActionAsync(action); menu.Items.Add(item); }
        control.ContextMenu = menu;
        control.PreviewMouseRightButtonDown += (_, e) =>
        {
            if (e.OriginalSource is not DependencyObject source) return;
            if (FindParent<DataGridRow>(source) is { } row) row.IsSelected = true;
            if (FindParent<ListBoxItem>(source) is { } tile) tile.IsSelected = true;
        };
        menu.Opened += (_, _) => { ((MenuItem)menu.Items[0]).IsEnabled = selected?.Entry.IsDirectory == true && !selected.Entry.IsLink; ((MenuItem)menu.Items[1]).IsEnabled = selected is not null && !selected.Entry.IsLink; ((MenuItem)menu.Items[2]).IsEnabled = selected?.Entry.IsDirectory == true && !selected.Entry.IsLink; ((MenuItem)menu.Items[3]).IsEnabled = selected is not null; };
    }
    private static T? FindParent<T>(DependencyObject source) where T : DependencyObject
    {
        while (source is not null) { if (source is T found) return found; source = VisualTreeHelper.GetParent(source); } return null;
    }
    public async Task<bool> NavigateAsync(string? location, bool addHistory = true)
    {
        if (disposed) return false;
        load?.Cancel(); load?.Dispose(); load = new(); var token = load.Token; int generation = ++request;
        loading = true; UpdateButtons(); message.Foreground = Muted; message.Text = "Reading folder…";
        try
        {
            DirectorySnapshot? result = null; List<BrowserItem> files = [];
            List<(string Label, string Path, ImageSource Icon)> folders = [];
            List<(string Name, string Label, string Type, long? Free, long? Total)> drives = [];
            bool showHidden = hidden.IsChecked == true;
            if (location is not null)
            {
                result = await browser.ReadAsync(location, showHidden, token);
                files = await Task.Run(() => result.Entries.Select(e => { token.ThrowIfCancellationRequested(); return new BrowserItem(e, ShellFileIcons.Get(e.FullPath, e.IsDirectory)); }).ToList(), token);
            }
            else await Task.Run(() =>
            {
                foreach (var (label, path) in PersonalFolders().Where(p => Directory.Exists(p.Path))) folders.Add((label, path, ShellFileIcons.Get(path, true)));
                foreach (var drive in DriveInfo.GetDrives().Where(d => d.DriveType != DriveType.Network))
                {
                    token.ThrowIfCancellationRequested();
                    try { drives.Add((drive.Name, drive.IsReady ? drive.VolumeLabel : "Not ready", drive.DriveType.ToString(), drive.IsReady ? drive.AvailableFreeSpace : null, drive.IsReady ? drive.TotalSize : null)); }
                    catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { drives.Add((drive.Name, "Unavailable", drive.DriveType.ToString(), null, null)); }
                }
            }, token);
            if (generation != request || disposed) return false;
            string? target = result?.Path;
            if (addHistory && !string.Equals(CurrentLocation, target, StringComparison.OrdinalIgnoreCase)) { back.Push(CurrentLocation); forward.Clear(); }
            CurrentLocation = target; snapshot = result; items = files; selected = null;
            search.IsEnabled = target is not null;
            search.Text = ""; address.Text = CurrentLocation ?? "This PC";
            if (target is null) ShowComputer(folders, drives); else { Filter(); ShowFiles(); }
            message.Text = result is null ? "This PC  •  " + Environment.MachineName : result.Truncated ? $"Showing the first {browser.MaxEntries:N0} entries. Narrow the folder to browse more." :
                result.Skipped > 0 ? $"{result.Skipped} entries could not be read." : "Browse files and folders. Double-click a folder to open it.";
            Select(null); LocationChanged?.Invoke(CurrentLocation); return true;
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested) { return false; }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or System.Security.SecurityException)
        {
            if (generation == request) { address.Text = CurrentLocation ?? "This PC"; message.Foreground = DesktopTheme.Color("#A42A2A"); message.Text = ex.Message; } return false;
        }
        finally { if (generation == request) { loading = false; UpdateButtons(); } }
    }
    public async Task BackAsync()
    {
        if (back.Count == 0) return; string? previous = CurrentLocation;
        if (await NavigateAsync(back.Peek(), false)) { back.Pop(); forward.Push(previous); UpdateButtons(); }
    }
    public async Task ForwardAsync()
    {
        if (forward.Count == 0) return; string? previous = CurrentLocation;
        if (await NavigateAsync(forward.Peek(), false)) { forward.Pop(); back.Push(previous); UpdateButtons(); }
    }
    public Task<bool> UpAsync() => NavigateAsync(CurrentLocation is null ? null : Path.GetDirectoryName(CurrentLocation));
    private void ShowFiles() => listing.Content = view.SelectedIndex == 1 ? tiles : table;
    public void SetFilter(string value) => search.Text = value;
    public void SetView(bool icons) => view.SelectedIndex = icons ? 1 : 0;
    private void Filter()
    {
        var visible = items.Where(i => i.Name.Contains(search.Text, StringComparison.OrdinalIgnoreCase)).ToArray();
        table.ItemsSource = visible; tiles.ItemsSource = visible; Select(null);
        summary.Text = CurrentLocation is null ? "Local computer" : $"{visible.Length:N0} items" + (snapshot?.Truncated == true ? "  •  listing limit reached" : "") + "  •  Search filters names in this folder";
    }
    public bool SelectByName(string name)
    {
        var item = items.FirstOrDefault(i => i.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        table.SelectedItem = item; tiles.SelectedItem = item; Select(item); return item is not null;
    }
    private void Select(BrowserItem? item)
    {
        selected = item; details.Children.Clear(); details.Children.Add(Text("Details", 15, true));
        if (item is null)
        {
            details.Children.Add(DesktopTheme.Icon(CurrentLocation is null ? Computer : Folder, 64)); details.Children.Add(new Border { Height = 16 });
            details.Children.Add(Text(CurrentLocation is null ? "This PC" : Path.GetFileName(CurrentLocation.TrimEnd('\\')), 18, true));
            details.Children.Add(Text(CurrentLocation ?? Environment.MachineName, 12, color: Muted));
            details.Children.Add(Text("Select an item to see its information and available actions.", 12, color: Muted));
        }
        else
        {
            details.Children.Add(new Image { Source = item.Icon, Width = 48, Height = 48, Margin = new Thickness(0, 12, 0, 16), HorizontalAlignment = HorizontalAlignment.Left });
            details.Children.Add(Text(item.Name, 17, true)); details.Children.Add(Text(item.Kind, 12, color: Muted));
            details.Children.Add(new Border { Height = 1, Background = Line, Margin = new Thickness(0, 12, 0, 12) });
            details.Children.Add(Text("Date modified", 11, color: Muted)); details.Children.Add(Text(item.Modified, 12));
            if (item.Entry.Size is not null) { details.Children.Add(Text("Size", 11, color: Muted)); details.Children.Add(Text(item.Size, 12)); }
            details.Children.Add(Text("Location", 11, color: Muted)); details.Children.Add(Text(item.Entry.FullPath, 12));
            if (item.Entry.IsLink) details.Children.Add(Text("Linked locations are not followed in this preview.", 12, color: Muted));
        }
        UpdateButtons();
    }
    private void UpdateButtons()
    {
        backButton.IsEnabled = !loading && back.Count > 0; forwardButton.IsEnabled = !loading && forward.Count > 0;
        upButton.IsEnabled = !loading && CurrentLocation is not null;
        scanButton.IsEnabled = !loading && (selected is not null || CurrentLocation is not null) && selected?.Entry.IsLink != true;
        preserveButton.IsEnabled = !loading && (selected?.Entry.IsDirectory == true || selected is null && CurrentLocation is not null) && selected?.Entry.IsLink != true;
        recoveryButton.IsEnabled = !loading;
    }
    private Task OpenAsync(BrowserItem item) => item.Entry.IsDirectory ? NavigateAsync(item.Entry.FullPath) : Task.CompletedTask;
    private Task ScanSelectedAsync() => selected is not null ? scan(selected.Entry.FullPath) : CurrentLocation is not null ? scan(CurrentLocation) : Task.CompletedTask;
    private Task PreserveSelectedAsync() => selected?.Entry.IsDirectory == true ? preserve(selected.Entry.FullPath) : selected is null && CurrentLocation is not null ? preserve(CurrentLocation) : Task.CompletedTask;
    private Task RecoverSelectedAsync() => recovery(selected?.Entry.FullPath ?? "");
    private void ShowComputer(List<(string Label, string Path, ImageSource Icon)> folders, List<(string Name, string Label, string Type, long? Free, long? Total)> drives)
    {
        var content = new StackPanel { Margin = new Thickness(24, 20, 24, 20) };
        content.Children.Add(Text("This PC", 25, true)); content.Children.Add(Text(Environment.MachineName, 12, color: Muted));
        content.Children.Add(Text("Folders", 16, true)); var folderTiles = new WrapPanel { Margin = new Thickness(0, 5, 0, 24) };
        foreach (var folder in folders)
        {
            var row = new DockPanel(); var icon = new Image { Source = folder.Icon, Width = 32, Height = 32, Margin = new Thickness(0, 0, 14, 0) }; DockPanel.SetDock(icon, Dock.Left); row.Children.Add(icon);
            row.Children.Add(new TextBlock { Text = folder.Label, FontSize = 14, VerticalAlignment = VerticalAlignment.Center });
            var button = new Button { Content = row, Width = 204, Padding = new Thickness(14), MinHeight = 66, Margin = new Thickness(0, 0, 12, 12), HorizontalContentAlignment = HorizontalAlignment.Left, Background = Brushes.White, BorderBrush = Line, ToolTip = folder.Path };
            Accessible(button, "Open " + folder.Label); button.Click += async (_, _) => await NavigateAsync(folder.Path); folderTiles.Children.Add(button);
        }
        content.Children.Add(folderTiles); content.Children.Add(Text("Devices and drives", 16, true)); var driveTiles = new WrapPanel();
        foreach (var drive in drives)
        {
            var row = new DockPanel(); var icon = DesktopTheme.Icon(drive.Type == "Removable" ? Usb : Drive, 40, Muted); icon.Margin = new Thickness(0, 0, 16, 0); DockPanel.SetDock(icon, Dock.Left); row.Children.Add(icon);
            var labels = new StackPanel(); labels.Children.Add(Text((string.IsNullOrWhiteSpace(drive.Label) ? "Local disk" : drive.Label) + " (" + drive.Name.TrimEnd('\\') + ")", 14, true));
            if (drive.Total is > 0 && drive.Free is { } free)
            {
                labels.Children.Add(new ProgressBar { Maximum = 100, Value = 100d * (drive.Total.Value - free) / drive.Total.Value, Height = 8, BorderThickness = new Thickness(0), Foreground = Blue, Background = DesktopTheme.Color("#E5EAF0"), Margin = new Thickness(0, 0, 0, 6) });
                labels.Children.Add(Text($"{free / (1024d * 1024 * 1024):F1} GB free of {drive.Total.Value / (1024d * 1024 * 1024):F1} GB", 11, color: Muted));
            }
            else labels.Children.Add(Text(drive.Type + "  •  Not ready", 12, color: Muted));
            row.Children.Add(labels); var button = new Button { Content = row, Width = 300, Padding = new Thickness(14), Margin = new Thickness(0, 8, 14, 8), HorizontalContentAlignment = HorizontalAlignment.Left, Background = Brushes.White, BorderBrush = Line, IsEnabled = drive.Free is not null };
            Accessible(button, "Open drive " + drive.Name); button.Click += async (_, _) => await NavigateAsync(drive.Name); driveTiles.Children.Add(button);
        }
        content.Children.Add(driveTiles); listing.Content = new ScrollViewer { Content = content, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled };
        summary.Text = $"{folders.Count} folders  •  {drives.Count} drives  •  Local computer";
    }
    public void Dispose() { disposed = true; request++; load?.Cancel(); load?.Dispose(); }
}
