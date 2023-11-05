namespace MessageQueueClient;

public interface IMessageQueueConsumer : IDisposable
{
    void StartListening<T>(string key, Func<T?, string, Task> messageHandler);
}