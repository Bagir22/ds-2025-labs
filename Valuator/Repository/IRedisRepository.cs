namespace Valuator.Repository;

public interface IRedisRepository
{
    string? Get(string key);
    bool Set(string key, string value);
    IEnumerable<string> GetKeys(string pattern);
}