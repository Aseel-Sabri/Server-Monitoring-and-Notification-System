namespace MessageProcessing;

public class AnomalyDetectionConfig
{
    public float MemoryUsageAnomalyThresholdPercentage { get; set; }
    public float CpuUsageAnomalyThresholdPercentage { get; set; }
    public float MemoryUsageThresholdPercentage { get; set; }
    public float CpuUsageThresholdPercentage { get; set; }
}