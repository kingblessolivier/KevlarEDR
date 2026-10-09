using System.Windows;
using System.Windows.Controls;
using System.Windows.Automation;
using System.Windows.Markup;
using System.Windows.Media;

namespace KevlarEDR.Desktop;

internal static class DesktopTheme
{
    public const string Computer = "\uE977", Shield = "\uE83D", Search = "\uE721",
        Drive = "\uEDA2", Usb = "\uE88E", Folder = "\uE8B7", History = "\uE81C",
        Settings = "\uE713", Refresh = "\uE72C", Document = "\uE8A5", Restore = "\uE777",
        Add = "\uE710", Warning = "\uE7BA", Chevron = "\uE76C";
    public static readonly string[] Glyphs = [Computer, Shield, Search, Drive, Usb, Folder, History, Settings, Refresh, Document, Restore, Add, Warning, Chevron];
    public static readonly FontFamily IconFont = new("Segoe Fluent Icons, Segoe MDL2 Assets");
    public static SolidColorBrush Color(string value) => new((System.Windows.Media.Color)ColorConverter.ConvertFromString(value));
    public static readonly Brush Ink = Color("#19283E"), Muted = Color("#607086"), Blue = Color("#1769E0"), Line = Color("#E2E8F0");

    public static TextBlock Icon(string glyph, double size = 20, Brush? color = null) => new()
    {
        Text = glyph, FontFamily = IconFont, FontSize = size, Foreground = color ?? Blue,
        VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center,
        IsHitTestVisible = false
    };
    public static TextBlock Text(string value, double size = 14, bool strong = false, Brush? color = null) => new()
    {
        Text = value, FontSize = size, FontWeight = strong ? FontWeights.SemiBold : FontWeights.Normal,
        Foreground = color ?? Ink, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 0, 0, 7)
    };
    public static Border IconTile(string glyph, double size = 24, string background = "#EBF3FF") => new()
    {
        Child = Icon(glyph, size), Background = Color(background), Width = size + 28, Height = size + 28,
        CornerRadius = new CornerRadius(12), VerticalAlignment = VerticalAlignment.Center
    };
    public static Border Card(UIElement content, double padding = 20) => new()
    {
        Child = content, Padding = new Thickness(padding), Margin = new Thickness(0, 0, 0, 16),
        CornerRadius = new CornerRadius(12), Background = Brushes.White, BorderBrush = Line, BorderThickness = new Thickness(1)
    };
    public static Border Badge(string value, bool warning = false) => new()
    {
        Background = Color(warning ? "#FFF0D7" : "#EAF2FE"), CornerRadius = new CornerRadius(6),
        Padding = new Thickness(10, 5, 10, 5), HorizontalAlignment = HorizontalAlignment.Left,
        Child = new TextBlock { Text = value, FontSize = 12, FontWeight = FontWeights.SemiBold,
            Foreground = Color(warning ? "#80520B" : "#205BAE") }
    };
    public static StackPanel Label(string glyph, string title, string detail)
    {
        var stack = new StackPanel(); var line = new DockPanel();
        var icon = Icon(glyph); icon.Margin = new Thickness(0, 0, 10, 7); DockPanel.SetDock(icon, Dock.Left);
        line.Children.Add(icon); line.Children.Add(Text(title, 16, true)); stack.Children.Add(line);
        stack.Children.Add(Text(detail, 13, color: Muted)); return stack;
    }
    public static void Accessible(DependencyObject element, string label) => AutomationProperties.SetName(element, label);

    public static ResourceDictionary Resources() => (ResourceDictionary)XamlReader.Parse("""
        <ResourceDictionary xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation" xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
          <Style TargetType="Button">
            <Setter Property="Background" Value="#FFFFFF"/><Setter Property="Foreground" Value="#273B55"/>
            <Setter Property="BorderBrush" Value="#DCE4EF"/><Setter Property="BorderThickness" Value="1"/>
            <Setter Property="Padding" Value="14,10"/><Setter Property="MinHeight" Value="40"/>
            <Setter Property="HorizontalContentAlignment" Value="Center"/><Setter Property="Cursor" Value="Hand"/>
            <Setter Property="Template"><Setter.Value><ControlTemplate TargetType="Button">
              <Border x:Name="Surface" Background="{TemplateBinding Background}" BorderBrush="{TemplateBinding BorderBrush}" BorderThickness="{TemplateBinding BorderThickness}" CornerRadius="7">
                <ContentPresenter Margin="{TemplateBinding Padding}" HorizontalAlignment="{TemplateBinding HorizontalContentAlignment}" VerticalAlignment="Center" RecognizesAccessKey="True"/>
              </Border>
              <ControlTemplate.Triggers>
                <Trigger Property="IsMouseOver" Value="True"><Setter TargetName="Surface" Property="Opacity" Value="0.82"/></Trigger>
                <Trigger Property="IsPressed" Value="True"><Setter TargetName="Surface" Property="Opacity" Value="0.65"/></Trigger>
                <Trigger Property="IsKeyboardFocused" Value="True"><Setter TargetName="Surface" Property="BorderBrush" Value="#1769E0"/><Setter TargetName="Surface" Property="BorderThickness" Value="2"/></Trigger>
                <Trigger Property="IsEnabled" Value="False"><Setter TargetName="Surface" Property="Opacity" Value="0.45"/></Trigger>
              </ControlTemplate.Triggers>
            </ControlTemplate></Setter.Value></Setter>
          </Style>
          <Style TargetType="ListBox">
            <Setter Property="Background" Value="White"/><Setter Property="BorderBrush" Value="#E2E8F0"/>
            <Setter Property="BorderThickness" Value="1"/><Setter Property="ScrollViewer.HorizontalScrollBarVisibility" Value="Disabled"/>
            <Setter Property="HorizontalContentAlignment" Value="Stretch"/>
          </Style>
          <Style TargetType="ListBoxItem">
            <Setter Property="Foreground" Value="#273B55"/><Setter Property="Padding" Value="12"/>
            <Setter Property="HorizontalContentAlignment" Value="Stretch"/>
            <Setter Property="Template"><Setter.Value><ControlTemplate TargetType="ListBoxItem">
              <Border x:Name="Surface" Background="Transparent" CornerRadius="7" Padding="{TemplateBinding Padding}" BorderThickness="1" BorderBrush="Transparent">
                <ContentPresenter HorizontalAlignment="{TemplateBinding HorizontalContentAlignment}"/>
              </Border>
              <ControlTemplate.Triggers>
                <Trigger Property="IsMouseOver" Value="True"><Setter TargetName="Surface" Property="Background" Value="#F0F5FC"/></Trigger>
                <Trigger Property="IsSelected" Value="True"><Setter TargetName="Surface" Property="Background" Value="#EAF2FE"/><Setter TargetName="Surface" Property="BorderBrush" Value="#B8D2F8"/></Trigger>
                <Trigger Property="IsKeyboardFocusWithin" Value="True"><Setter TargetName="Surface" Property="BorderBrush" Value="#1769E0"/></Trigger>
              </ControlTemplate.Triggers>
            </ControlTemplate></Setter.Value></Setter>
          </Style>
          <Style x:Key="NavigationItem" TargetType="ListBoxItem">
            <Setter Property="Foreground" Value="#34445C"/><Setter Property="Padding" Value="12,10"/>
            <Setter Property="HorizontalContentAlignment" Value="Stretch"/><Setter Property="Margin" Value="0,1"/>
            <Setter Property="Template"><Setter.Value><ControlTemplate TargetType="ListBoxItem">
              <Border x:Name="Surface" Background="Transparent" CornerRadius="8" Padding="{TemplateBinding Padding}" BorderThickness="1" BorderBrush="Transparent">
                <ContentPresenter/>
              </Border>
              <ControlTemplate.Triggers>
                <Trigger Property="IsMouseOver" Value="True"><Setter TargetName="Surface" Property="Background" Value="#EEF1F5"/></Trigger>
                <Trigger Property="IsSelected" Value="True"><Setter TargetName="Surface" Property="Background" Value="#E3EDFB"/><Setter Property="Foreground" Value="#185ABA"/></Trigger>
                <Trigger Property="IsKeyboardFocusWithin" Value="True"><Setter TargetName="Surface" Property="BorderBrush" Value="#1769E0"/></Trigger>
              </ControlTemplate.Triggers>
            </ControlTemplate></Setter.Value></Setter>
          </Style>
          <Style TargetType="TextBox">
            <Setter Property="Background" Value="#FAFCFF"/><Setter Property="Foreground" Value="#19283E"/>
            <Setter Property="BorderBrush" Value="#D5DFED"/><Setter Property="Padding" Value="12,10"/>
            <Setter Property="SelectionBrush" Value="#B8D2F8"/>
          </Style>
        </ResourceDictionary>
        """);
}
