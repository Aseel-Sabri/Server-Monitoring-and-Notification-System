using System.Diagnostics;

namespace ServerStatisticsCollection;

public class WindowsStatisticsCollector : IServerStatisticsCollector
{
    private readonly PerformanceCounter _cpuCounter = new("Processor", "% Processor Time", "_Total");
    private readonly PerformanceCounter _availableMemoryCounter = new("Memory", "Available MBytes");
    private readonly PerformanceCounter _committedBytesCounter = new("Memory", "Committed Bytes");

    public float GetCpuUsage()
    {
        return _cpuCounter.NextValue() / 100;
    }

    public float GetUsedMemory()
    {
        return _committedBytesCounter.NextValue() / 1000000 - GetAvailableMemory();
    }

    public float GetAvailableMemory()
    {
        return _availableMemoryCounter.NextValue();
    }
}