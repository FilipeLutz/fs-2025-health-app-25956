namespace HealthApp.Domain.EventBus;

public interface IEventBus
{
    void Publish(string queueName, string message);
    void Subscribe(string queueName, Action<string> handler);
    void Dispose();
}