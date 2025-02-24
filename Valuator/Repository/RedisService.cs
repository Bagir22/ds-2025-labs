using StackExchange.Redis;
using System.Collections.Generic;
using System.Linq;

namespace Valuator.Repository
{
    public class RedisRepository : IRedisRepository
    {
        private readonly IDatabase _redis;
        private readonly IServer _server;
        
        public RedisRepository(IConnectionMultiplexer redisMultiplexer)
        {
            var endPoint = redisMultiplexer.GetEndPoints().First();
            _server = redisMultiplexer.GetServer(endPoint);
            _redis = redisMultiplexer.GetDatabase();
        }

        public string? Get(string key)
        {
            return _redis.StringGet(key);
        }

        public bool Set(string key, string value)
        {
            return _redis.StringSet(key, value);
        }

        public IEnumerable<string> GetKeys(string pattern = "*")
        {
            return _server.Keys(pattern: pattern).Select(k => k.ToString());
        }
    }
}