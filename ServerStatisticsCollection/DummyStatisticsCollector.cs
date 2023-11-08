namespace ServerStatisticsCollection;

public class DummyStatisticsCollector : IServerStatisticsCollector
{
    private readonly Random _random = new Random();

    public float GetCpuUsage()
    {
        return _random.NextSingle();
    }

    public float GetUsedMemory()
    {
        return _random.NextSingle() * 10000;
    }

    public float GetAvailableMemory()
    {
        return _random.NextSingle() * 10000;
    }
}