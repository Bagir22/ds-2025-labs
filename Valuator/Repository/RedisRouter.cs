using StackExchange.Redis;

namespace Valuator.Repository;

public class RedisRouter
{
    private readonly Dictionary<string, IConnectionMultiplexer> _shards = new();
    private readonly IConnectionMultiplexer _main;

    public RedisRouter(IConnectionMultiplexer main, IConfiguration configuration)
    {
        _main = main;

        _shards = new Dictionary<string, IConnectionMultiplexer>
        {
            { "RU", ConnectionMultiplexer.Connect(GetConnectionString("DB_RU", "redis_ru:6380")) },
            { "EU", ConnectionMultiplexer.Connect(GetConnectionString("DB_EU", "redis_eu:6381")) },
            { "ASIA", ConnectionMultiplexer.Connect(GetConnectionString("DB_ASIA", "redis_asia:6382")) }
        };
    }
    
    private string GetConnectionString(string environmentVariable, string defaultValue)
    {
        // Читаем значение из переменной окружения, если она существует
        var dbHost = Environment.GetEnvironmentVariable(environmentVariable) ?? defaultValue;
        var dbPassword = Environment.GetEnvironmentVariable("DB_PASSWORD") ?? "somePassword"; 

        return $"{dbHost},password={dbPassword}";
    }
    
    public IDatabase GetShard(string countryCode)
    {
        countryCode = countryCode.ToUpper();

        if (!_shards.ContainsKey(countryCode))
        {
            string connectionString = countryCode switch
            {
                "RU" => GetConnectionString("DB_RU", "redis_ru:6380"),
                "EU" => GetConnectionString("DB_EU", "redis_eu:6381"),
                "ASIA" => GetConnectionString("DB_ASIA", "redis_asia:6382"),
                _ => GetConnectionString("DB_MAIN", "redis_main:6379")
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