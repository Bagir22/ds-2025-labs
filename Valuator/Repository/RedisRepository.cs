using StackExchange.Redis;
using System.Collections.Generic;
using System.Linq;

namespace Valuator.Repository
{
    public class RedisRepository : IRedisRepository
    {
        private readonly RedisRouter _router;

        public RedisRepository(RedisRouter router)
        {
            _router = router;
        }

        public string? Get(string shardKey, string key)
        {
            IDatabase db;
            if (shardKey == "main")
            {
                db = _router.GetMainDb();
            }
            else
            {
                db = _router.GetShard(shardKey);
            }
            
            return db.StringGet(key);
        }

        public bool Set(string shardKey, string key, string value)
        {
            IDatabase db;
            if (shardKey == "main")
            {
                db = _router.GetMainDb();
            }
            else
            {
                db = _router.GetShard(shardKey);
            }
            
            return db.StringSet(key, value);
        }
    }
}