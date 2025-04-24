namespace Valuator.Repository;

public interface IRedisRepository
{
    string? Get(string shardKey, string key);
    bool Set(string shardKey, string key, string value);
}