using System.Text;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using StackExchange.Redis;
using System.Threading;

namespace RankCalculator;

class Program
{
    static void Main(string[] args)
    {
        try
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
            var redis = redisConnection.GetDatabase();
            Console.WriteLine("Connected to Redis");

            Console.WriteLine("Connecting to RabbitMQ");

            var rabbitHost = Environment.GetEnvironmentVariable("RABBITMQ_HOST") ?? "localhost";
            var rabbitUser = Environment.GetEnvironmentVariable("RABBITMQ_USER") ?? "guest";
            var rabbitPassword = Environment.GetEnvironmentVariable("RABBITMQ_PASSWORD") ?? "guest";

            var connectionFactory = new ConnectionFactory()
            {
                HostName = rabbitHost,
                UserName = rabbitUser,
                Password = rabbitPassword,
            };
            
            while (true)
            {
                try
                {
                    IConnection connection = connectionFactory.CreateConnection();
                    IModel channel = connection.CreateModel();

                    channel.QueueDeclare(queue: "valuator.processing.rank",
                        durable: true,
                        exclusive: false,
                        autoDelete: false,
                        arguments: null);

                    var consumer = new EventingBasicConsumer(channel);
                    consumer.Received += (model, ea) =>
                    {
                        try
                        {
                            var body = ea.Body.ToArray();
                            var id = Encoding.UTF8.GetString(body); 

                            string textKey = $"TEXT-{id}";
                            string text = redis.StringGet(textKey);

                            if (!string.IsNullOrEmpty(text))
                            {
                                double rank = CalculateRank(text);
                                string rankKey = $"RANK-{id}";
                                Thread.Sleep(500);
                                redis.StringSet(rankKey, rank.ToString());

                                Console.WriteLine($"Processed ID: {id}, Rank: {rank}");
                            }

                            channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error while processing message: {ex.Message}");
                        }
                    };

                    channel.BasicConsume(queue: "valuator.processing.rank", autoAck: false, consumer: consumer);
                        
                    Console.WriteLine("Waiting for messages");
                    while (true)
                    {
                        Thread.Sleep(1000);
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
        catch (Exception ex) {
            Console.WriteLine($"Error in rabbitmq setup: {ex.Message}");
        }
    }    
    static double CalculateRank(string text)
    {
        int total = text.Length;
        int nonLetters = text.Count(ch => !char.IsLetter(ch));
        return Math.Round((double)nonLetters / total, 2);
    }
}