namespace Valuator.Publisher;

public interface IRabbitMqPublisher
{
    void Publish(string exchange, string routingKey, string message);
}