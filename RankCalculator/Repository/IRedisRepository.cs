namespace RankCalculator.Repository;

public interface IRedisRepository
{
    string? Get(string shardKey, string key);
}