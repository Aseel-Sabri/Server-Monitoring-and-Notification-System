using System.Text.RegularExpressions;
using MessageQueueClient;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;

namespace MessageProcessing;

public class MessageProcessingService : IHostedService
{
    private readonly IMessageQueueConsumer _messageQueueConsumer;
    private readonly IServerStatisticsRepository _statisticsRepository;
    private readonly AnomalyDetectionConfig _anomalyDetectionConfig;
    private readonly IHubContext<ServerHub> _serverHub;
    private readonly ILogger<MessageProcessingService> _logger;
    private const string Key = "ServerStatistics.*"; // TODO


    public MessageProcessingService(
        IMessageQueueConsumer messageQueueConsumer,
        IServerStatisticsRepository statisticsRepository,
        IOptions<AnomalyDetectionConfig> anomalyDetectionOptions,
        IHubContext<ServerHub> serverHub, ILogger<MessageProcessingService> logger)
    {
        _messageQueueConsumer = messageQueueConsumer;
        _statisticsRepository = statisticsRepository;
        _serverHub = serverHub;
        _logger = logger;
        _anomalyDetectionConfig = anomalyDetectionOptions.Value;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            _messageQueueConsumer.StartListening<ServerStatistics>(Key, async (serverStatistics, routingKey) =>
            {
                if (serverStatistics != null)
                {
                    var serverIdentifier = GetServerIdentifier(routingKey);
                    serverStatistics.ServerIdentifier = serverIdentifier;

                    var previousServerStatistics =
                        await _statisticsRepository.GetMostRecentServerStatistic(serverIdentifier);

                    await _statisticsRepository.AddServerStatistics(serverStatistics);
                    
                    await SendAnomalyAlerts(serverStatistics, previousServerStatistics, cancellationToken);
                }
            });
        }
        catch (Exception e)
        {
            _logger.LogError(e.Message);
            throw;
        }

        return Task.CompletedTask;
    }

    private async Task SendAnomalyAlerts(ServerStatistics currentStatistics, ServerStatistics? previousStatistics,
        CancellationToken cancellationToken)
    {
        if (IsCpuAnomaly(currentStatistics, previousStatistics))
            await _serverHub.Clients.All.SendAsync("CpuAnomaly", currentStatistics.ServerIdentifier,
                cancellationToken: cancellationToken);

        if (IsCpuHighUsage(currentStatistics))
            await _serverHub.Clients.All.SendAsync("CpuHighUsage", currentStatistics.ServerIdentifier,
                cancellationToken: cancellationToken);

        if (IsMemoryAnomaly(currentStatistics, previousStatistics))
            await _serverHub.Clients.All.SendAsync("MemoryAnomaly", currentStatistics.ServerIdentifier,
                cancellationToken: cancellationToken);

        if (IsMemoryHighUsage(currentStatistics))
            await _serverHub.Clients.All.SendAsync("MemoryHighUsage", currentStatistics.ServerIdentifier,
                cancellationToken: cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _messageQueueConsumer.Dispose();
        return Task.CompletedTask;
    }

    private string GetServerIdentifier(string routingKey)
    {
        var match = Regex.Match(Key, "^(.*?)\\..*$");
        if (!match.Success || match.Groups.Count < 2)
            return string.Empty;

        var identifierMatch = Regex.Match(routingKey, $"^{match.Groups[1].Value}\\.(.*)$");

        if (identifierMatch is { Success: false, Groups.Count: < 2 })
            return string.Empty;

        return identifierMatch.Groups[1].Value;
    }

    private bool IsMemoryAnomaly(ServerStatistics currentStatistics, ServerStatistics? previousStatistics)
    {
        if (previousStatistics is null)
            return false;

        return currentStatistics.MemoryUsage >
               (previousStatistics.MemoryUsage * (1 + _anomalyDetectionConfig.MemoryUsageAnomalyThresholdPercentage));
    }

    private bool IsMemoryHighUsage(ServerStatistics currentStatistics)
    {
        return (currentStatistics.MemoryUsage /
                (currentStatistics.MemoryUsage + currentStatistics.AvailableMemory)) >
               _anomalyDetectionConfig.MemoryUsageThresholdPercentage;
    }

    private bool IsCpuAnomaly(ServerStatistics currentStatistics, ServerStatistics? previousStatistics)
    {
        if (previousStatistics is null)
            return false;

        return currentStatistics.CpuUsage >
               (previousStatistics.CpuUsage * (1 + _anomalyDetectionConfig.CpuUsageAnomalyThresholdPercentage));
    }

    private bool IsCpuHighUsage(ServerStatistics currentStatistics)
    {
        return currentStatistics.CpuUsage > _anomalyDetectionConfig.CpuUsageThresholdPercentage;
    }
}