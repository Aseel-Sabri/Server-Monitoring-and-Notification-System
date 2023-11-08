using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Retry;
using RabbitMQ.Client;

namespace MessageQueueClient;

public class RabbitMQPublisher : IMessageQueuePublisher
{
    private readonly RabbitMQConfig _config;
    private readonly IConnectionFactory _connectionFactory;
    private readonly RetryPolicy _retryPolicy;

    public RabbitMQPublisher(IOptions<RabbitMQConfig> options, IConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
        _config = options.Value;
        _retryPolicy = Policy
            .Handle<Exception>()
            .WaitAndRetry(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));
    }

    private IModel CreateChannel(IConnection connection)
    {
        var channel = connection.CreateModel();
        channel.ExchangeDeclare(exchange: _config.ExchangeName, type: _config.ExchangeType);
        return channel;
    }

    public void PublishMessage<T>(T entity, string key) where T : class
    {
        _retryPolicy.Execute(() =>
        {
            using var connection = _connectionFactory.CreateConnection();
            using var channel = CreateChannel(connection);
            var message = JsonSerializer.Serialize(entity);

            var body = Encoding.UTF8.GetBytes(message);

            channel.BasicPublish(exchange: _config.ExchangeName,
                routingKey: key,
                basicProperties: null,
                body: body);
        });
    }
}