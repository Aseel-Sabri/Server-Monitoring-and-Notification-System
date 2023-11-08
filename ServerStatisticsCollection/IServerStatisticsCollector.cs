namespace ServerStatisticsCollection;

public interface IServerStatisticsCollector
{
    float GetCpuUsage();
    float GetUsedMemory();
    float GetAvailableMemory();
}