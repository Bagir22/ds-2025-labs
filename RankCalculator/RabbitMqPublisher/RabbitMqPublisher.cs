using System.Text;
using RabbitMQ.Client;

namespace Valuator.Publisher;

public class RabbitMqPublisher : IRabbitMqPublisher, IDisposable
{
    private readonly IConnection _connection;
    private readonly IModel _channel;

    public RabbitMqPublisher()
    {
        var factory = new ConnectionFactory()
        {
            HostName = Environment.GetEnvironmentVariable("RABBITMQ_HOST") ?? "rabbitmq",
            UserName = Environment.GetEnvironmentVariable("RABBITMQ_USER") ?? "guest",
            Password = Environment.GetEnvironmentVariable("RABBITMQ_PASS") ?? "guest"
        };

        _connection = factory.CreateConnection();
        _channel = _connection.CreateModel();
    }

    public void Publish(string exchange, string routingKey, string message)
    {
        try
        {
            var body = Encoding.UTF8.GetBytes(message);
            _channel.BasicPublish(exchange: exchange, routingKey: routingKey, body: body);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"RabbitMQ publish error: {ex.Message}");
        }
    }
    
    public void Dispose()
    {
        _channel?.Close();
        _channel?.Dispose();
        _connection?.Close();
        _connection?.Dispose();
    }
}