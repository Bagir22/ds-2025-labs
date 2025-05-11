using System;
using System.Text;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace EventsLogger
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Starting EventsLogger...");

            var factory = new ConnectionFactory()
            {
                HostName = Environment.GetEnvironmentVariable("RABBITMQ_HOST") ?? "rabbitmq",
                UserName = Environment.GetEnvironmentVariable("RABBITMQ_USER") ?? "guest",
                Password = Environment.GetEnvironmentVariable("RABBITMQ_PASS") ?? "guest",
                AutomaticRecoveryEnabled = true
            };

            while (true)
            {
                try
                {
                    using (var connection = factory.CreateConnection())
                    using (var channel = connection.CreateModel())
                    {
                        Console.WriteLine("Connected to RabbitMQ");
                        
                        channel.ExchangeDeclare("valuator.events", ExchangeType.Direct, durable: true);
                        
                        var rankQueue = channel.QueueDeclare("eventslogger.rank", durable: false, exclusive: false, autoDelete: false);
                        channel.QueueBind(rankQueue, "valuator.events", "rank");

                        var similarityQueue = channel.QueueDeclare("eventslogger.similarity", durable: false, exclusive: false, autoDelete: false);
                        channel.QueueBind(similarityQueue, "valuator.events", "similarity");
                        
                        var lookupQueue = channel.QueueDeclare("eventslogger.lookup", durable: false, exclusive: false, autoDelete: false);
                        channel.QueueBind(lookupQueue, "valuator.events", "lookup");
                        
                        var rankConsumer = new EventingBasicConsumer(channel);
                        rankConsumer.Received += (model, ea) =>
                        {
                            var body = ea.Body.ToArray();
                            var message = Encoding.UTF8.GetString(body);
                            Console.WriteLine($"[Rank] {DateTime.Now}: {message}");
                        };

                        var similarityConsumer = new EventingBasicConsumer(channel);
                        similarityConsumer.Received += (model, ea) =>
                        {
                            var body = ea.Body.ToArray();
                            var message = Encoding.UTF8.GetString(body);
                            Console.WriteLine($"[Similarity] {DateTime.Now}: {message}");
                        };

                        var lookupConsumer = new EventingBasicConsumer(channel);
                        lookupConsumer.Received += (model, ea) =>
                        {
                            var body = ea.Body.ToArray();
                            var message = Encoding.UTF8.GetString(body);
                            Console.WriteLine($"[Lookup] {DateTime.Now}: {message}");
                        };
                        
                        channel.BasicConsume(rankQueue, true, rankConsumer);
                        channel.BasicConsume(similarityQueue, true, similarityConsumer);
                        channel.BasicConsume(lookupQueue, true, lookupConsumer);
                        
                        while (connection.IsOpen)
                        {
                            Thread.Sleep(1000);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error: {ex.Message}");
                    Console.WriteLine("Reconnecting in 5 seconds...");
                    Thread.Sleep(5000);
                }
            }
        }
    }
}