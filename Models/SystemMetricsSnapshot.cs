namespace QuickTools.Models;

public sealed class SystemMetricsSnapshot
{
    public static SystemMetricsSnapshot Empty { get; } = new();

    public int CpuUsagePercent { get; init; }
    public double? CpuTemperatureCelsius { get; init; }
    public int? GpuUsagePercent { get; init; }
    public double? GpuTemperatureCelsius { get; init; }
    public int RamUsagePercent { get; init; }
    public int DiskUsagePercent { get; init; }
    public double NetworkDownloadBytesPerSecond { get; init; }
    public double NetworkUploadBytesPerSecond { get; init; }
}
