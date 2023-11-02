using System.Diagnostics;
using Microsoft.Extensions.Options;
using MessageQueueClient;

namespace ServerStatisticsCollection;

public class ServerStatisticsService : BackgroundService
{
    private readonly ILogger<ServerStatisticsService> _logger;
    private readonly IMessageQueuePublisher _publisher;
    private readonly string _topic;
    private readonly int _samplingIntervalMs;
    private readonly PerformanceCounter _cpuCounter;
    private readonly PerformanceCounter _availableMemoryCounter;
    private readonly PerformanceCounter _committedBytesCounter;


    public ServerStatisticsService(ILogger<ServerStatisticsService> logger, IOptions<ServerStatisticsConfig> options,
        IMessageQueuePublisher publisher)
    {
        _logger = logger;
        _publisher = publisher;
        _topic = $"ServerStatistics.{options.Value.ServerIdentifier}";
        _samplingIntervalMs = options.Value.SamplingIntervalSeconds * 1000;

        // TODO
        _cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
        _availableMemoryCounter = new PerformanceCounter("Memory", "Available MBytes");
        _committedBytesCounter = new PerformanceCounter("Memory", "Committed Bytes");
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var serverStatistics = GetServerStatistics();
            _publisher.PublishMessage(serverStatistics, _topic);
            _logger.LogInformation($"{DateTime.Now}: Message Sent");

            await Task.Delay(_samplingIntervalMs, stoppingToken);
        }
    }

    private ServerStatistics GetServerStatistics()
    {
        var cpuUsage = _cpuCounter.NextValue();
        var availableMemory = _availableMemoryCounter.NextValue();
        var usedMemory = _committedBytesCounter.NextValue() / 1000000 - availableMemory;

        return new ServerStatistics()
        {
            Timestamp = DateTime.Now,
            CpuUsage = cpuUsage,
            AvailableMemory = availableMemory,
            MemoryUsage = usedMemory
        };
    }
}