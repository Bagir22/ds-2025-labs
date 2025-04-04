using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RabbitMQ.Client;
using StackExchange.Redis;
using Valuator.Repository;
using Valuator.Publisher;

namespace Valuator.Pages;

public class IndexModel : PageModel
{
    private readonly ILogger<IndexModel> _logger;
    private readonly IRedisRepository _redis;
    private readonly IRabbitMqPublisher _publisher;

    public IndexModel(ILogger<IndexModel> logger, IRedisRepository redis, IRabbitMqPublisher publisher)
    {
        _logger = logger;
        _redis = redis;
        _publisher = publisher;
    }

    public void OnGet()
    {
        
    }

    public IActionResult OnPost(string text)
    {
        _logger.LogDebug(text);
        string id = Guid.NewGuid().ToString();

        if (!string.IsNullOrEmpty(text))
        {
            string textKey = $"TEXT-{id}";
            _redis.Set(textKey, text);

            string similarityKey = $"SIMILARITY-{id}";
            int similarity = CalculateSimilarity(text, id);
            _redis.Set(similarityKey, similarity.ToString());

            _publisher.Publish("valuator.processing.rank", id);
        }

        return Redirect($"summary?id={id}");
    }
    
    private int CalculateSimilarity(string text, string id)
    {
        var keys = _redis.GetKeys("TEXT-*");

        foreach (var key in keys)
        {
            if (_redis.Get(key) == text && key.Replace("TEXT-", "") != id)
            {
                return 1;
            }
        }

        return 0;
    }
}