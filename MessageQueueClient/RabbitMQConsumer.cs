using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Retry;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace MessageQueueClient;

public class RabbitMQConsumer : IMessageQueueConsumer
{
    private readonly RabbitMQConfig _config;
    private readonly IConnectionFactory _connectionFactory;
    private readonly RetryPolicy _retryPolicy;
    private IConnection? _connection;
    private IModel? _channel;

    public RabbitMQConsumer(IOptions<RabbitMQConfig> options, IConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
        _config = options.Value;
        _retryPolicy = Policy
            .Handle<Exception>()
            .WaitAndRetry(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));
    }

    private void CreateChannel()
    {
        _retryPolicy.Execute(() =>
        {
            _connection = _connectionFactory.CreateConnection();
            _channel = _connection.CreateModel();
            _channel.ExchangeDeclare(exchange: _config.ExchangeName, type: _config.ExchangeType);
        });
    }

    public void StartListening<T>(string key, Func<T?, string, Task> messageHandler)
    {
        CreateChannel();

        var queueName = _channel.QueueDeclare().QueueName;

        _channel.QueueBind(queue: queueName, exchange: _config.ExchangeName, routingKey: key);

        var consumerAsync = new AsyncEventingBasicConsumer(_channel);

        consumerAsync.Received += async (_, eventArgs) =>
        {
            var body = eventArgs.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);
            var entity = JsonSerializer.Deserialize<T>(message);
            var routingKey = eventArgs.RoutingKey;

            await messageHandler(entity, routingKey);
        };

        _channel.BasicConsume(queue: queueName, autoAck: true, consumer: consumerAsync);
    }

    public void Dispose()
    {
        _channel?.Dispose();
        _connection?.Dispose();
    }
}