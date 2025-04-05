using System.Text;
using RabbitMQ.Client;

namespace Valuator.Publisher;

public class RabbitMqPublisher : IRabbitMqPublisher
{
    private readonly IConnection _connection;
    private readonly IModel _channel;

    public RabbitMqPublisher()
    {
        var connectionFactory = new ConnectionFactory()
        {
            HostName = "rabbitmq",
            UserName = "admin",
            Password = "adminPassword"
        };

        _connection = connectionFactory.CreateConnection();
        _channel = _connection.CreateModel();

        _channel.QueueDeclare(queue: "valuator.processing.rank",
            durable: true,
            exclusive: false,
            autoDelete: false, // TODO за что отвечает
            arguments: null);
    }

    public void Publish(string queue, string message)
    {
        try
        {
            var properties = _channel.CreateBasicProperties();
            properties.Persistent = true;

            byte[] data = Encoding.UTF8.GetBytes(message);
            _channel.BasicPublish(exchange: "", routingKey: queue, basicProperties: properties, body: data);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"RabbitMQ publish error: {ex.Message}");
        }
    }
}