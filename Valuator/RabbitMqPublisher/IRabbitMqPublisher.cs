namespace Valuator.Publisher;

public interface IRabbitMqPublisher
{
    void Publish(string queue, string message);
}