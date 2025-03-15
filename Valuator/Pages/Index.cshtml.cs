using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using StackExchange.Redis;
using Valuator.Repository;

namespace Valuator.Pages;

public class IndexModel : PageModel
{
    private readonly ILogger<IndexModel> _logger;
    private readonly IRedisRepository _redis;

    public IndexModel(ILogger<IndexModel> logger, IRedisRepository redis)
    {
        _logger = logger;
        _redis = redis;   
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
            string rankKey = $"RANK-{id}";
            string similarityKey = $"SIMILARITY-{id}";
        
            double rank = CalculateRank(text);
            int similarity = CalculateSimilarity(text);
            
            _redis.Set(textKey, text);
            _redis.Set(rankKey, rank.ToString());
            //_logger.LogInformation($"Similarity: {similarity}");
            _redis.Set(similarityKey, similarity.ToString());
        }
        
        return Redirect($"summary?id={id}");
    }

    private double CalculateRank(string text)
    {
        int total = text.Length;

        int nonLetters = text.Count(ch => !char.IsLetter(ch));
        return (double)nonLetters / total;
    }

    private int CalculateSimilarity(string text)
    {
        var keys = _redis.GetKeys("TEXT-*");
        //_logger.LogInformation(string.Join(", ", keys.Select(k => k.ToString())));

        foreach (var key in keys)
        {
            if (_redis.Get(key) == text)
            {
                return 1;
            }
        }

        return 0;
    }
}