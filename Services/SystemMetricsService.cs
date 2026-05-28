using System.Diagnostics;
using System.IO;
using System.Management;
using Microsoft.VisualBasic.Devices;
using QuickTools.Models;

namespace QuickTools.Services;

public sealed class SystemMetricsService : IDisposable
{
    private readonly PerformanceCounter? _cpuCounter;
    private readonly PerformanceCounter? _diskCounter;
    private readonly List<PerformanceCounter> _networkReceivedCounters = [];
    private readonly List<PerformanceCounter> _networkSentCounters = [];
    private readonly ComputerInfo _computerInfo = new();
    private bool _disposed;

    public SystemMetricsService()
    {
        _cpuCounter = CreateCounter("Processor", "% Processor Time", "_Total");
        _diskCounter = CreateCounter("PhysicalDisk", "% Disk Time", "_Total");

        try
        {
            foreach (var instance in new PerformanceCounterCategory("Network Interface").GetInstanceNames())
            {
                if (instance.Contains("loopback", StringComparison.OrdinalIgnoreCase)
                    || instance.Contains("isatap", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var received = CreateCounter("Network Interface", "Bytes Received/sec", instance);
                var sent = CreateCounter("Network Interface", "Bytes Sent/sec", instance);

                if (received is not null)
                {
                    _networkReceivedCounters.Add(received);
                }

                if (sent is not null)
                {
                    _networkSentCounters.Add(sent);
                }
            }
        }
        catch
        {
        }

        _ = _cpuCounter?.NextValue();
        _ = _diskCounter?.NextValue();
        foreach (var counter in _networkReceivedCounters.Concat(_networkSentCounters))
        {
            _ = counter.NextValue();
        }
    }

    public async Task<SystemMetricsSnapshot> GetSnapshotAsync()
    {
        return await Task.Run(() =>
        {
            var gpuInfo = TryReadNvidiaGpuInfo();
            var totalMemory = _computerInfo.TotalPhysicalMemory;
            var availableMemory = _computerInfo.AvailablePhysicalMemory;
            var usedMemoryPercent = totalMemory == 0
                ? 0
                : ClampPercent((double)(totalMemory - availableMemory) / totalMemory * 100);

            return new SystemMetricsSnapshot
            {
                CpuUsagePercent = ReadCounterPercent(_cpuCounter),
                CpuTemperatureCelsius = TryReadTemperature("CPU"),
                GpuUsagePercent = gpuInfo.UsagePercent ?? TryReadGpuUsage(),
                GpuTemperatureCelsius = gpuInfo.TemperatureCelsius ?? TryReadTemperature("GPU"),
                RamUsagePercent = usedMemoryPercent,
                DiskUsagePercent = ReadCounterPercent(_diskCounter),
                NetworkDownloadBytesPerSecond = SumCounters(_networkReceivedCounters),
                NetworkUploadBytesPerSecond = SumCounters(_networkSentCounters)
            };
        });
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _cpuCounter?.Dispose();
        _diskCounter?.Dispose();

        foreach (var counter in _networkReceivedCounters.Concat(_networkSentCounters))
        {
            counter.Dispose();
        }
    }

    private static PerformanceCounter? CreateCounter(string categoryName, string counterName, string instanceName)
    {
        try
        {
            return new PerformanceCounter(categoryName, counterName, instanceName, readOnly: true);
        }
        catch
        {
            return null;
        }
    }

    private static int ReadCounterPercent(PerformanceCounter? counter)
    {
        if (counter is null)
        {
            return 0;
        }

        try
        {
            return ClampPercent(counter.NextValue());
        }
        catch
        {
            return 0;
        }
    }

    private static double SumCounters(IEnumerable<PerformanceCounter> counters)
    {
        double total = 0;

        foreach (var counter in counters)
        {
            try
            {
                total += counter.NextValue();
            }
            catch
            {
            }
        }

        return total;
    }

    private static int? TryReadGpuUsage()
    {
        try
        {
            var counters = new PerformanceCounterCategory("GPU Engine")
                .GetInstanceNames()
                .Where(instance => instance.Contains("engtype_3D", StringComparison.OrdinalIgnoreCase))
                .Select(instance => CreateCounter("GPU Engine", "Utilization Percentage", instance))
                .Where(counter => counter is not null)
                .ToList();

            if (counters.Count == 0)
            {
                return null;
            }

            foreach (var counter in counters)
            {
                _ = counter!.NextValue();
            }

            Thread.Sleep(80);
            var usage = counters.Sum(counter =>
            {
                try
                {
                    return counter!.NextValue();
                }
                finally
                {
                    counter?.Dispose();
                }
            });

            return ClampPercent(usage);
        }
        catch
        {
            return null;
        }
    }

    private static double? TryReadTemperature(string hardwareName)
    {
        try
        {
            using var searcher = new ManagementObjectSearcher(
                @"root\OpenHardwareMonitor",
                "SELECT Value FROM Sensor WHERE SensorType='Temperature' AND Name LIKE '%" + hardwareName + "%'");

            var values = searcher.Get()
                .Cast<ManagementObject>()
                .Select(item => Convert.ToDouble(item["Value"]))
                .Where(value => value > 0)
                .ToList();

            return values.Count > 0 ? values.Average() : null;
        }
        catch
        {
            return null;
        }
    }

    private static bool MatchesHardware(ManagementBaseObject item, IReadOnlyCollection<string> hardwareTokens)
    {
        var name = Convert.ToString(item["Name"]) ?? "";
        var identifier = Convert.ToString(item["Identifier"]) ?? "";
        var source = $"{name} {identifier}";

        return hardwareTokens.Any(token => source.Contains(token, StringComparison.OrdinalIgnoreCase));
    }

    private static double? TryReadAcpiThermalZoneTemperature()
    {
        try
        {
            using var searcher = new ManagementObjectSearcher(
                @"root\WMI",
                "SELECT CurrentTemperature FROM MSAcpi_ThermalZoneTemperature");

            var values = searcher.Get()
                .Cast<ManagementObject>()
                .Select(item => (Convert.ToDouble(item["CurrentTemperature"]) / 10d) - 273.15d)
                .Where(value => value is > 0 and < 130)
                .ToList();

            return values.Count > 0 ? values.Average() : null;
        }
        catch
        {
            return null;
        }
    }

    private static (int? UsagePercent, double? TemperatureCelsius) TryReadNvidiaGpuInfo()
    {
        try
        {
            var executable = FindNvidiaSmiPath();
            if (string.IsNullOrWhiteSpace(executable))
            {
                return (null, null);
            }

            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = executable,
                    Arguments = "--query-gpu=utilization.gpu,temperature.gpu --format=csv,noheader,nounits",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                }
            };

            process.Start();
            if (!process.WaitForExit(800))
            {
                process.Kill(entireProcessTree: true);
                return (null, null);
            }

            var output = process.StandardOutput.ReadToEnd().Trim();
            var firstLine = output.Split('\n', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault()?.Trim();
            if (string.IsNullOrWhiteSpace(firstLine))
            {
                return (null, null);
            }

            var parts = firstLine.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            int? usage = parts.Length > 0 && double.TryParse(parts[0], out var parsedUsage)
                ? ClampPercent(parsedUsage)
                : null;
            var temperature = parts.Length > 1 && double.TryParse(parts[1], out var parsedTemperature)
                ? parsedTemperature
                : (double?)null;

            return (usage, temperature);
        }
        catch
        {
            return (null, null);
        }
    }

    private static string? FindNvidiaSmiPath()
    {
        var candidates = new[]
        {
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), @"NVIDIA Corporation\NVSMI\nvidia-smi.exe"),
            "nvidia-smi.exe"
        };

        return candidates.FirstOrDefault(candidate =>
        {
            try
            {
                return candidate.Equals("nvidia-smi.exe", StringComparison.OrdinalIgnoreCase) || File.Exists(candidate);
            }
            catch
            {
                return false;
            }
        });
    }

    private static int ClampPercent(double value)
    {
        if (double.IsNaN(value) || double.IsInfinity(value))
        {
            return 0;
        }

        return (int)Math.Clamp(Math.Round(value), 0, 100);
    }
}
