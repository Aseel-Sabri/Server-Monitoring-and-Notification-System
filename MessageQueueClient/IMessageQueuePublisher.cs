namespace MessageQueueClient;

public interface IMessageQueuePublisher
{
    void PublishMessage<T>(T entity, string key) where T : class;
}