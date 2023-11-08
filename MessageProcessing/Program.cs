using MessageProcessing;
using MessageQueueClient;
using RabbitMQ.Client;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSignalR();

var configuration = builder.Configuration;
configuration.AddEnvironmentVariables();
var rabbitMqConfigSection = configuration.GetSection(nameof(RabbitMQConfig));

builder.Services
    .Configure<RabbitMQConfig>(rabbitMqConfigSection)
    .Configure<MongoDBConfig>(configuration.GetSection(nameof(MongoDBConfig)))
    .Configure<AnomalyDetectionConfig>(configuration.GetSection(nameof(AnomalyDetectionConfig)))
    .AddSingleton<IConnectionFactory>(sp => new ConnectionFactory
    {
        HostName = rabbitMqConfigSection.GetSection("HostName").Value,
        DispatchConsumersAsync = true
    })
    .AddSingleton<IServerStatisticsRepository, ServerStatisticsRepository>()
    .AddSingleton<IMessageQueueConsumer, RabbitMQConsumer>()
    .AddHostedService<MessageProcessingService>();

var app = builder.Build();

app.MapGet("/", () => "Hello World!");

var hubUrl = configuration.GetSection("SignalRConfig").GetSection("SignalRUrl");
app.MapHub<ServerHub>(hubUrl.Value);
app.Run();