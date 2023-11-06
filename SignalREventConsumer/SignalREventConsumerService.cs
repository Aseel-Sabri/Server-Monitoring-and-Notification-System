using Microsoft.AspNetCore.SignalR.Client;
using Polly;
using Polly.Retry;

namespace SignalREventConsumer;

public class SignalREventConsumerService : BackgroundService
{
    private readonly ILogger<SignalREventConsumerService> _logger;
    private readonly HubConnection _hubConnection;

    public SignalREventConsumerService(ILogger<SignalREventConsumerService> logger, HubConnection hubConnection)
    {
        _logger = logger;
        _hubConnection = hubConnection;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        HandleHubEvents();

        var policy = Policy
            .Handle<Exception>()
            .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));

        await StartHubConnection(policy, stoppingToken);

        _hubConnection.Closed += async (error) =>
        {
            _logger.LogWarning("Connection closed. Retrying...");
            await StartHubConnection(policy, stoppingToken);
        };
    }

    private void HandleHubEvents()
    {
        _hubConnection.On<string>("CpuAnomaly",
            serverIdentifier => { Console.WriteLine($"CPU Anomaly Alert received for server: {serverIdentifier}"); });

        _hubConnection.On<string>("CpuHighUsage",
            serverIdentifier =>
            {
                Console.WriteLine($"CPU High Usage Alert received for server: {serverIdentifier}");
            });

        _hubConnection.On<string>("MemoryAnomaly",
            serverIdentifier =>
            {
                Console.WriteLine($"Memory Anomaly Alert received for server: {serverIdentifier}");
            });

        _hubConnection.On<string>("MemoryHighUsage",
            serverIdentifier =>
            {
                Console.WriteLine($"Memory High Usage Alert received for server: {serverIdentifier}");
            });
    }

    private async Task StartHubConnection(AsyncRetryPolicy policy, CancellationToken stoppingToken)
    {
        try
        {
            await policy.ExecuteAsync(async () =>
            {
                await _hubConnection.StartAsync(stoppingToken);
                _logger.LogInformation("SignalR connection started");
            });
        }
        catch (Exception ex)
        {
            _logger.LogError($"All retry attempts failed. Unable to connect to SignalR hub. Error: {ex.Message}");
            throw;
        }
    }
}