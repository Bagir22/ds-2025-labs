using System;
using System.Linq;
using System.Text;
using System.Threading;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using StackExchange.Redis;
using Microsoft.AspNetCore.SignalR.Client;
using RankCalculator.Repository;
using Valuator.Publisher;

namespace RankCalculator
{
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
        private readonly RedisRouter _router;
        private readonly IModel _channel;
        private readonly IConnection _connection;

        public MessageProcessor(RedisRouter router, IModel channel, IConnection connection)
        {
            _router = router;
            _channel = channel;
            _connection = connection;
        }

        public void ProcessMessage(BasicDeliverEventArgs ea)
        {
            try
            {
                using var publishChannel = _connection.CreateModel();
                
                var body = ea.Body.ToArray();
                var id = Encoding.UTF8.GetString(body);

                var redisRepo = new RedisRepository(_router);
                
                var countryCode = redisRepo.Get("main", $"TEXT-{id}");
                publishChannel.BasicPublish(
                    exchange: "valuator.events",
                    routingKey: "lookup",
                    body: Encoding.UTF8.GetBytes($"[LOOKUP]: TEXT-{id}, main"));
                if (string.IsNullOrEmpty(countryCode))
                {
                    Console.WriteLine($"Region not found for ID: {id}");
                    _channel.BasicAck(ea.DeliveryTag, false);
                    return;
                }
                
                string text = redisRepo.Get(countryCode, $"TEXT-{id}");
                publishChannel.BasicPublish(
                    exchange: "valuator.events",
                    routingKey: "lookup",
                    body: Encoding.UTF8.GetBytes($"[LOOKUP]: TEXT-{id}, {countryCode}"));
                if (string.IsNullOrEmpty(text))
                {
                    Console.WriteLine($"Text hash not found for ID: {id} in {countryCode} region");
                    _channel.BasicAck(ea.DeliveryTag, false);
                    return;
                }
                
                var random = new Random();
                Thread.Sleep(random.Next(3000, 15000));

                double rank = RankCalculator.Calculate(text);
                string rankKey = $"RANK-{id}";

                var db = _router.GetShard(countryCode);
                db.StringSet(rankKey, rank.ToString());

                PublishRankCalculated(id, rank);
                NotifyClient(id);

                Console.WriteLine($"Processed ID: {id}, Rank: {rank}");

                _channel.BasicAck(ea.DeliveryTag, false);
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
                    exchange: "valuator.events",
                    routingKey: "rank",
                    body: body);

                Console.WriteLine($"Published RankCalculated event for ID: {id}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error publishing RankCalculated event: {ex.Message}");
            }
        }

        private async void NotifyClient(string id)
        {
            try
            {
                var connection = new HubConnectionBuilder()
                    .WithUrl("http://nginx:8000/hub")
                    .Build();

                await connection.StartAsync();
                await connection.InvokeAsync("SendAsync", "ReceiveResult", id);
                Console.WriteLine($"SignalR notified: {id}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"SignalR notify failed: {ex.Message}");
            }
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

    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                var mainHost = Environment.GetEnvironmentVariable("DB_MAIN") ?? "redis_main:6379";
                var password = Environment.GetEnvironmentVariable("DB_PASSWORD");

                var mainConfig = $"{mainHost},password={password},abortConnect=false";
                var mainConnection = ConnectionMultiplexer.Connect(mainConfig);

                var router = new RedisRouter(mainConnection);

                var connectionFactory = RabbitMqConnector.GetConnectionFactory();
                StartMessageProcessing(connectionFactory, router);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in application setup: {ex.Message}");
            }
        }

        private static void StartMessageProcessing(ConnectionFactory connectionFactory, RedisRouter router)
        {
            while (true)
            {
                try
                {
                    using var connection = connectionFactory.CreateConnection();
                    using var channel = connection.CreateModel();

                    SetupQueue(channel);
                    var processor = new MessageProcessor(router, channel, connection);

                    var consumer = new EventingBasicConsumer(channel);
                    consumer.Received += (model, ea) => processor.ProcessMessage(ea);

                    channel.BasicConsume(queue: "valuator.processing.rank", autoAck: false, consumer: consumer);

                    Console.WriteLine("Waiting for messages...");
                    while (connection.IsOpen)
                    {
                        Thread.Sleep(1000);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error connecting to RabbitMQ: {ex.Message}");
                    Console.WriteLine("Retrying in 5 seconds...");
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
}
