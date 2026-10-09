namespace KevlarEDR.Core;

public sealed record LocalVolume(string Root, DriveType Type, bool Ready, string Label);
public sealed record VolumeChange(string Kind, LocalVolume Volume);

/// <summary>Compares successful inventories; the first inventory is a silent baseline.</summary>
public sealed class VolumeTracker
{
    private Dictionary<string, LocalVolume>? previous;
    public IReadOnlyList<VolumeChange> Update(IEnumerable<LocalVolume> volumes)
    {
        var current = volumes.Where(v => v.Type != DriveType.Network)
            .ToDictionary(v => v.Root, StringComparer.OrdinalIgnoreCase);
        var changes = new List<VolumeChange>();
        if (previous is not null)
        {
            foreach (var volume in current.Values)
            {
                if (!previous.TryGetValue(volume.Root, out var old)) changes.Add(new("Connected", volume));
                else if (volume.Ready != old.Ready) changes.Add(new(volume.Ready ? "Ready" : "Unavailable", volume));
            }
            foreach (var volume in previous.Values)
                if (!current.ContainsKey(volume.Root)) changes.Add(new("Removed", volume));
        }
        previous = current;
        return changes;
    }
    public static IReadOnlyList<LocalVolume> Read()
    {
        var result = new List<LocalVolume>();
        foreach (var drive in DriveInfo.GetDrives())
        {
            if (drive.DriveType == DriveType.Network) continue;
            bool ready = false; string label = "";
            try { ready = drive.IsReady; if (ready) label = drive.VolumeLabel; }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { }
            result.Add(new(drive.Name, drive.DriveType, ready, label));
        }
        return result;
    }
}
