using Microsoft.AspNetCore.SignalR.Client;
using SignalREventConsumer;

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureAppConfiguration(builder => { builder.AddEnvironmentVariables(); })
    .ConfigureServices((hostContext, services) =>
    {
        IConfiguration configuration = hostContext.Configuration;
        var hubUrl = configuration.GetSection("SignalRConfig").GetSection("SignalRUrl");
        services
            .AddSingleton<HubConnection>(sp =>
                new HubConnectionBuilder()
                    .WithUrl(hubUrl.Value)
                    .Build())
            .AddHostedService<SignalREventConsumerService>();
    })
    .Build();
host.Run();