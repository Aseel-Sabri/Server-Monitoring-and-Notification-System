using MessageQueueClient;
using RabbitMQ.Client;
using ServerStatisticsCollection;

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((hostContext, services) =>
    {
        IConfiguration configuration = hostContext.Configuration;

        var configSection = configuration.GetSection(nameof(RabbitMQConfig));
        services
            .Configure<RabbitMQConfig>(configSection)
            .Configure<ServerStatisticsConfig>(configuration.GetSection(nameof(ServerStatisticsConfig)))
            .AddSingleton<IConnectionFactory>(sp => new ConnectionFactory
            {
                HostName = configSection.GetSection("HostName").Value,
                DispatchConsumersAsync = true
            })
            .AddSingleton<IMessageQueuePublisher, RabbitMQPublisher>()
            .AddSingleton<IServerStatisticsCollector, WindowsStatisticsCollector>()
            .AddHostedService<ServerStatisticsService>();
    })
    .Build();

host.Run();