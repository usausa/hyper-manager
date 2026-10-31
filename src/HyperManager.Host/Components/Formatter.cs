namespace HyperManager.Host.Components;

using MudBlazor;

public static class Formatter
{
    private const double BytesPerGigabyte = 1024d * 1024d * 1024d;

    //--------------------------------------------------------------------------------
    // VM
    //--------------------------------------------------------------------------------

    public static string FormatCpu(int? cpu) =>
        cpu?.ToString(CultureInfo.CurrentCulture) ?? "-";

    public static string FormatMemory(long? memoryMb) =>
        memoryMb?.ToString("N0", CultureInfo.CurrentCulture) ?? "-";

    public static string FormatUptime(TimeSpan? uptime)
    {
        if (uptime is null)
        {
            return "-";
        }

        var value = uptime.Value;
        return value.Days > 0
            ? $"{value.Days}d {value.Hours:D2}:{value.Minutes:D2}:{value.Seconds:D2}"
            : $"{value.Hours:D2}:{value.Minutes:D2}:{value.Seconds:D2}";
    }

    //--------------------------------------------------------------------------------
    // Host
    //--------------------------------------------------------------------------------

    public static string FormatPercent(double value) =>
        value.ToString("F0", CultureInfo.CurrentCulture) + "%";

    public static string FormatMegabytes(long megabytes) =>
        (megabytes / 1024d).ToString("F1", CultureInfo.CurrentCulture) + " GB";

    public static string FormatBytes(long bytes) =>
        (bytes / BytesPerGigabyte).ToString("F0", CultureInfo.CurrentCulture) + " GB";

    public static double MemoryUsedPercent(HostMetrics metrics) =>
        metrics.TotalMemoryMb <= 0 ? 0 : 100d * metrics.UsedMemoryMb / metrics.TotalMemoryMb;

    public static double DiskUsedPercent(DiskInfo disk) =>
        disk.TotalBytes <= 0 ? 0 : 100d * (disk.TotalBytes - disk.FreeBytes) / disk.TotalBytes;

    public static Color UsageColor(double percent) => percent switch
    {
        >= 90 => Color.Error,
        >= 75 => Color.Warning,
        _ => Color.Primary
    };
}
