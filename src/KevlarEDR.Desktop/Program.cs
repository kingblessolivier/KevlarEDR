using System.IO;
using System.Windows;

namespace KevlarEDR.Desktop;

internal static class Program
{
    [STAThread]
    private static int Main(string[] args)
    {
        try
        {
            bool smoke = args.Contains("--smoke");
            bool created = true;
            using var instance = smoke ? null : new Mutex(true, "Local\\KevlarEDR." + DesktopSession.UserKey, out created);
            // A fresh process uses the existing window instead of starting a second observer.
            if (!smoke && !created)
            {
                if (!args.Contains("--background")) DesktopSession.SendToExisting(args.Contains("--exit"));
                return 0;
            }
            if (args.Contains("--exit")) return 0;
            string root = smoke ? Path.Combine(Path.GetTempPath(), "KevlarEDR-smoke-" + Guid.NewGuid().ToString("N"))
                : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "KevlarEDR");
            var app = new Application();
            var window = new MainWindow(root, !smoke);
            using var session = smoke ? null : new DesktopSession(window);
            app.SessionEnding += (_, _) => window.Quit();
            if (smoke) window.Loaded += async (_, _) =>
            {
                try
                {
                    await Task.Delay(1500);
                    string report = args.Length > 1 && args[0] == "--smoke" ? Path.GetFullPath(args[1])
                        : Path.Combine(root, "smoke.json");
                    await window.SmokeAsync(report);
                }
                catch (Exception ex) { window.SmokeFailure = ex.Message; }
                finally { window.Close(); }
            };
            if (args.Contains("--background"))
            {
                app.MainWindow = window;
                window.Show(); window.Hide();
                app.Run();
            }
            else app.Run(window);
            return window.SmokeFailure is null ? 0 : 1;
        }
        catch (Exception ex)
        {
            if (!args.Contains("--smoke")) MessageBox.Show(ex.Message, "KevlarEDR could not start", MessageBoxButton.OK, MessageBoxImage.Error);
            return 1;
        }
    }
}
