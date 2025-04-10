using System;
using System.Text;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using StackExchange.Redis;
using System.Threading;
using System.Linq;

namespace RankCalculator
{
    public static class RedisConnector
    {
        public static IDatabase ConnectToRedis()
        {
            var redisHost = Environment.GetEnvironmentVariable("REDIS_HOST") ?? "localhost";
            var redisPort = Environment.GetEnvironmentVariable("REDIS_PORT") ?? "6379";
            var redisPassword = Environment.GetEnvironmentVariable("REDIS_PASSWORD") ?? "";

            var redisConnectionString = $"{redisHost}:{redisPort}";
            if (!string.IsNullOrEmpty(redisPassword))
            {
                redisConnectionString += $",password={redisPassword}";
            }

            var redisConnection = ConnectionMultiplexer.Connect(redisConnectionString);
            Console.WriteLine("Connected to Redis");
            return redisConnection.GetDatabase();
        }
    }

    public static class RabbitMqConnector
    {
        public static ConnectionFactory GetConnectionFactory()
        {
            var rabbitHost = Environment.GetEnvironmentVariable("RABBITMQ_HOST") ?? "rabbitmq";
            var rabbitUser = Environment.GetEnvironmentVariable("RABBITMQ_USER") ?? "guest";
            var rabbitPassword = Environment.GetEnvironmentVariable("RABBITMQ_PASS") ?? "guest";

            return new ConnectionFactory()
            {
                HostName = rabbitHost,
                UserName = rabbitUser,
                Password = rabbitPassword,
            };
        }
    }
    
    public class MessageProcessor
    {
        private readonly IDatabase _redis;
        private readonly IModel _channel;
        private readonly IConnection _connection;

        public MessageProcessor(IDatabase redis, IModel channel, IConnection connection)
        {
            _redis = redis;
            _channel = channel;
            _connection = connection;
        }

        public void ProcessMessage(BasicDeliverEventArgs ea)
        {
            try
            {
                var body = ea.Body.ToArray();
                var id = Encoding.UTF8.GetString(body);

                string textKey = $"TEXT-{id}";
                string text = _redis.StringGet(textKey);

                if (!string.IsNullOrEmpty(text))
                {
                    double rank = RankCalculator.Calculate(text);
                    string rankKey = $"RANK-{id}";
                    _redis.StringSet(rankKey, rank.ToString());
                    
                    PublishRankCalculated(id, rank);

                    Console.WriteLine($"Processed ID: {id}, Rank: {rank}");
                }

                _channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error while processing message: {ex.Message}");
            }
        }

        private void PublishRankCalculated(string id, double rank)
        {
            try
            {
                using var publishChannel = _connection.CreateModel();

                var message = $"{id}:{rank}";
                var body = Encoding.UTF8.GetBytes(message);
        
                publishChannel.BasicPublish(
                    exchange: "valuator.events.rank",
                    routingKey: "",
                    body: body);
        
                Console.WriteLine($"Published RankCalculated event for ID: {id}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error publishing RankCalculated event: {ex.Message}");
            }
        }

    }

    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                var redis = RedisConnector.ConnectToRedis();
                var connectionFactory = RabbitMqConnector.GetConnectionFactory();

                StartMessageProcessing(connectionFactory, redis);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in application setup: {ex.Message}");
            }
        }

        private static void StartMessageProcessing(ConnectionFactory connectionFactory, IDatabase redis)
        {
            while (true)
            {
                try
                {
                    using (var connection = connectionFactory.CreateConnection())
                    using (var channel = connection.CreateModel())
                    {
                        SetupQueue(channel);
                        var processor = new MessageProcessor(redis, channel, connection);

                        var consumer = new EventingBasicConsumer(channel);
                        consumer.Received += (model, ea) => processor.ProcessMessage(ea);

                        channel.BasicConsume(queue: "valuator.processing.rank", autoAck: false, consumer: consumer);

                        Console.WriteLine("Waiting for messages");
                        while (connection.IsOpen)
                        {
                            Thread.Sleep(1000);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error connecting to RabbitMQ: {ex.Message}");
                    Console.WriteLine("Retrying in 5 seconds");
                    Thread.Sleep(5000);
                }
            }
        }

        private static void SetupQueue(IModel channel)
        {
            channel.ExchangeDeclare(
                exchange: "valuator.processing.rank",
                type: ExchangeType.Fanout,
                durable: true);

            channel.QueueDeclare(
                queue: "valuator.processing.rank",
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null);
            
            channel.QueueBind(
                queue: "valuator.processing.rank",
                exchange: "valuator.processing.rank",
                routingKey: ""); 
        }
    }
    
    public static class RankCalculator
    {
        public static double Calculate(string text)
        {
            int total = text.Length;
            int nonLetters = text.Count(ch => !char.IsLetter(ch));
            return Math.Round((double)nonLetters / total, 2);
        }
    }
}