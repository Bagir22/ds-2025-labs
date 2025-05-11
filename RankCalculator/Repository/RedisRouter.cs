using StackExchange.Redis;

namespace RankCalculator.Repository;

public class RedisRouter
{
    private readonly Dictionary<string, IConnectionMultiplexer> _shards = new();
    private readonly IConnectionMultiplexer _main;

    public RedisRouter(IConnectionMultiplexer main)
    {
        _main = main;

        _shards = new Dictionary<string, IConnectionMultiplexer>
        {
            { "RU", ConnectionMultiplexer.Connect($"{Environment.GetEnvironmentVariable("DB_RU")},password={Environment.GetEnvironmentVariable("DB_PASSWORD")},abortConnect=false") },
            { "EU", ConnectionMultiplexer.Connect($"{Environment.GetEnvironmentVariable("DB_EU")},password={Environment.GetEnvironmentVariable("DB_PASSWORD")},abortConnect=false") },
            { "ASIA", ConnectionMultiplexer.Connect($"{Environment.GetEnvironmentVariable("DB_ASIA")},password={Environment.GetEnvironmentVariable("DB_PASSWORD")},abortConnect=false") }
        };
    }

    public IDatabase GetShard(string countryCode)
    {
        countryCode = countryCode.ToUpper();

        if (!_shards.ContainsKey(countryCode))
        {
            string connectionString = countryCode switch
            {
                "RU" => $"{Environment.GetEnvironmentVariable("DB_RU")}",
                "EU" => $"{Environment.GetEnvironmentVariable("DB_EU")}",
                "ASIA" => $"{Environment.GetEnvironmentVariable("DB_ASIA")}",
                _ => $"{Environment.GetEnvironmentVariable("DB_MAIN")}"
            };

            connectionString = $"{connectionString},password={Environment.GetEnvironmentVariable("DB_PASSWORD")}";

            var connection = ConnectionMultiplexer.Connect(connectionString);
            _shards[countryCode] = connection;
        }

        return _shards[countryCode].GetDatabase();
    }

    public Dictionary<string, IConnectionMultiplexer> GetAllShards()
    {
        return _shards;
    }

    public IDatabase GetMainDb() => _main.GetDatabase();
}