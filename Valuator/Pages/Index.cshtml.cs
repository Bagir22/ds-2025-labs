using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RabbitMQ.Client;
using Valuator.Repository;
using Valuator.Publisher;
using Valuator.Models;

namespace Valuator.Pages;

public class IndexModel : PageModel
{
    private readonly ILogger<IndexModel> _logger;
    private readonly IRedisRepository _redis;
    private readonly IRabbitMqPublisher _publisher;
    public List<Country> Countries { get; set; } = new();

    public IndexModel(ILogger<IndexModel> logger, IRedisRepository redis, IRabbitMqPublisher publisher)
    {
        _logger = logger;
        _redis = redis;
        _publisher = publisher;
    }

    public void OnGet()
    {
        Countries = Country.AllCountries;
    }

    public IActionResult OnPost(string text)
    {
        Countries = Country.AllCountries;
        
        string countryCode = Request.Form["country"];
        
        string id = Guid.NewGuid().ToString();

        if (!string.IsNullOrEmpty(text))
        {
            var textHash = GetTextHash(text);
            string similarity = GetSimilarity(ref countryCode, textHash, ref id); 
            
            _publisher.Publish(
                exchange: "valuator.events",
                routingKey: "similarity",
                message: $"[SIMILARITY]-{id}:{similarity}");
            
            if (similarity == "0")
            {
                _redis.Set("main", $"TEXT-{id}", countryCode); 
                
                _redis.Set(countryCode, $"TEXT_HASH-{textHash}", id);
                _redis.Set(countryCode, $"TEXT-{id}", text);
                _redis.Set(countryCode, $"SIMILARITY-{textHash}", "0");
            }
            else
            {
                _redis.Set(countryCode, $"SIMILARITY-{textHash}", "1");
            }
            
            _publisher.Publish(
               exchange: "valuator.events",
               routingKey: "similarity",
               message: $"[SIMILARITY]-{id}:{similarity}");
            
            _publisher.Publish(
                exchange: "valuator.processing.rank",
                routingKey: "",
                message: $"{id}");
        }
        
        Console.WriteLine(id);
        return Redirect($"summary?id={id}");
    }
    
    private string GetSimilarity(ref string countryCode, string textHash, ref string id)
    {
        foreach (var shard in new[] { "RU", "EU", "ASIA"})
        {
            var existingKey = _redis.Get(shard, $"SIMILARITY-{textHash}");
            _publisher.Publish(
                exchange: "valuator.events",
                routingKey: "lookup",
                message: $"[LOOKUP]: SIMILARITY-{textHash}, {shard}");
            if (!string.IsNullOrEmpty(existingKey))
            {
                _logger.LogInformation($"Found SIMILARITY-{textHash} in {shard} shard");
                id = _redis.Get(shard, $"TEXT_HASH-{textHash}");
                _publisher.Publish(
                    exchange: "valuator.events",
                    routingKey: "lookup",
                    message: $"[LOOKUP]: TEXT_HASH-{textHash}, {shard}");
                countryCode = shard;
                _redis.Set(shard, $"SIMILARITY-{textHash}", "1");
                return "1";
            }
        }
        
        return "0"; 
    }
    
    private string GetTextHash(string text)
    {
        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(text));
        return BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
    }
}
