using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace KevlarEDR.Desktop;

internal static class ShellFileIcons
{
    private static readonly Dictionary<string, ImageSource> cache = new(StringComparer.OrdinalIgnoreCase);
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct ShellInfo
    {
        public IntPtr Icon; public int Index; public uint Attributes;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)] public string DisplayName;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)] public string TypeName;
    }
    [DllImport("shell32.dll", CharSet = CharSet.Unicode, EntryPoint = "SHGetFileInfoW")]
    private static extern IntPtr GetFileInfo(string path, uint attributes, out ShellInfo info, uint size, uint flags);
    [DllImport("user32.dll")] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool DestroyIcon(IntPtr icon);
    [DllImport("ole32.dll")] private static extern int CoInitializeEx(IntPtr reserved, uint mode);
    [DllImport("ole32.dll")] private static extern void CoUninitialize();

    // Call on the browser worker, not the UI thread. Use only an extension and
    // synthetic attributes: no user-file icon handlers, content reads, or previews.
    public static ImageSource Get(string path, bool folder)
    {
        string key = folder ? "<folder>" : Path.GetExtension(path).ToLowerInvariant();
        if (key.Length > 24) key = "";
        lock (cache)
        {
            if (cache.TryGetValue(key, out var cached)) return cached;
            if (cache.Count >= 128 && key != "<folder>") key = "";
            if (cache.TryGetValue(key, out cached)) return cached;
            int initialized = CoInitializeEx(IntPtr.Zero, 0);
            try
            {
                if (GetFileInfo(folder ? "folder" : "file" + key, folder ? 0x10u : 0x80u,
                    out var info, (uint)Marshal.SizeOf<ShellInfo>(), 0x100 | 0x10) == IntPtr.Zero || info.Icon == IntPtr.Zero)
                    throw new IOException("Windows could not supply a file-type icon.");
                try
                {
                    var image = Imaging.CreateBitmapSourceFromHIcon(info.Icon, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
                    image.Freeze(); cache[key] = image; return image;
                }
                finally { DestroyIcon(info.Icon); }
            }
            finally { if (initialized >= 0) CoUninitialize(); }
        }
    }
}
