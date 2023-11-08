using System.Diagnostics;
using Microsoft.Extensions.Options;
using MessageQueueClient;

namespace ServerStatisticsCollection;

public class ServerStatisticsService : BackgroundService
{
    private readonly ILogger<ServerStatisticsService> _logger;
    private readonly IMessageQueuePublisher _publisher;
    private readonly IServerStatisticsCollector _statisticsCollector;
    private readonly string _topic;
    private readonly int _samplingIntervalMs;

    public ServerStatisticsService(ILogger<ServerStatisticsService> logger, IOptions<ServerStatisticsConfig> options,
        IMessageQueuePublisher publisher, IServerStatisticsCollector statisticsCollector)
    {
        _logger = logger;
        _publisher = publisher;
        _statisticsCollector = statisticsCollector;
        _topic = $"ServerStatistics.{options.Value.ServerIdentifier}";
        _samplingIntervalMs = options.Value.SamplingIntervalSeconds * 1000;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var serverStatistics = GetServerStatistics();
                _publisher.PublishMessage(serverStatistics, _topic);
                _logger.LogInformation($"{DateTime.Now}: Message Sent");
            }
            catch (Exception e)
            {
                _logger.LogError(e.Message);
                throw;
            }

            await Task.Delay(_samplingIntervalMs, stoppingToken);
        }
    }

    private ServerStatistics GetServerStatistics()
    {
        var cpuUsage = _statisticsCollector.GetCpuUsage();
        var availableMemory = _statisticsCollector.GetAvailableMemory();
        var usedMemory = _statisticsCollector.GetUsedMemory();

        return new ServerStatistics()
        {
            Timestamp = DateTime.Now,
            CpuUsage = cpuUsage,
            AvailableMemory = availableMemory,
            MemoryUsage = usedMemory
        };
    }
}