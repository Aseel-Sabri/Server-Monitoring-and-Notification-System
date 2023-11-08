namespace MessageProcessing;

public class ServerStatistics
{
    public string ServerIdentifier { get; set; }
    public double MemoryUsage { get; set; } // in MB
    public double AvailableMemory { get; set; } // in MB
    public double CpuUsage { get; set; }
    public DateTime Timestamp { get; set; }
}