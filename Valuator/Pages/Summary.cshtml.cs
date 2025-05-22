using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using Valuator.Publisher;
using Valuator.Repository;

namespace Valuator.Pages;

[Authorize]
public class SummaryModel : PageModel
{
    private readonly ILogger<SummaryModel> _logger;
    private readonly IRedisRepository _redis;
    private readonly IRabbitMqPublisher _publisher;
    public SummaryModel(ILogger<SummaryModel> logger,  IRedisRepository redis, IRabbitMqPublisher publisher)
    {
        _logger = logger;
        _redis = redis; 
        _publisher = publisher;
    }

    public double? Rank { get; set; }
    public double? Similarity { get; set; }
    public void OnGet(string id)
    {
        _logger.LogDebug(id);

        string? countryCode = _redis.Get("main", $"TEXT-{id}");
        if (string.IsNullOrEmpty(countryCode))
        {
            Response.StatusCode = 404;
            Response.WriteAsync("Not Found");
            return;
        }
        
        var authorId = _redis.Get(countryCode, $"TEXT-AUTHOR-{id}");
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (authorId != currentUserId)
        {
            Response.StatusCode = 403;
            Response.WriteAsync("Forbidden");
            return;
        }
        
        string rankValue = GetRank(id);
        string similarityValue = GetSimiliarity(id);

        if (!double.TryParse(rankValue, out double rank))
        {
            Rank = null;
        }
        else
        {
            Rank = rank;
        }

        if (!double.TryParse(similarityValue, out double similarity))
        {
            Similarity = null;
        }
        else
        {
            Similarity = similarity;
        }
    }
    
    private string GetSimiliarity(string id)
    {
        string? countryCode = _redis.Get("main", $"TEXT-{id}");
        _publisher.Publish(
            exchange: "valuator.events",
            routingKey: "lookup",
            message: $"[LOOKUP]: TEXT-{id}, main");
        if (string.IsNullOrEmpty(countryCode))
        {
            _logger.LogWarning($"Region not found for ID: {id}");
            return "-1";
        }

        string? text = _redis.Get(countryCode, $"TEXT-{id}");
        _publisher.Publish(
            exchange: "valuator.events",
            routingKey: "lookup",
            message: $"[LOOKUP]: TEXT-{id}, {countryCode}");
        if (string.IsNullOrEmpty(text))
        {
            _logger.LogWarning($"Text not found for ID: {id} in {countryCode} region");
            return "-1";
        }
        
        string textHash = GetTextHash(text);

        foreach (var shard in new[] { "RU", "EU", "ASIA"})
        {
            var existingKey = _redis.Get(shard, $"SIMILARITY-{textHash}");
            _publisher.Publish(
                exchange: "valuator.events",
                routingKey: "lookup",
                message: $"[LOOKUP]: SIMILARITY-{textHash}, {shard}");
            if (!string.IsNullOrEmpty(existingKey) && existingKey == "1")
            {
                _logger.LogInformation($"Found SIMILARITY-{textHash} in {shard} shard");
                return existingKey;
            }
        }

        _logger.LogWarning($"SIMILARITY-{textHash} not found in any shard");
        return "0";
    }
    
    private string GetRank(string id)
    {
        string? countryCode = _redis.Get("main", $"TEXT-{id}");
        _publisher.Publish(
            exchange: "valuator.events",
            routingKey: "lookup",
            message: $"[LOOKUP]: TEXT-{id}, main");
        if (string.IsNullOrEmpty(countryCode))
        {
            _logger.LogWarning($"Region not found for ID: {id}");
            return "Оценка содержания не завершена";
        }

        string? rank = _redis.Get(countryCode, $"RANK-{id}");
        _publisher.Publish(
            exchange: "valuator.events",
            routingKey: "lookup",
            message: $"[LOOKUP]: RANK-{id}, {countryCode}");
        if (string.IsNullOrEmpty(rank))
        {
            _logger.LogWarning($"Text not found for ID: {id} in {countryCode} region");
            return "Оценка содержания не завершена";
        }

        return rank;
    }
    
    private string GetTextHash(string text)
    {
        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(text));
        return BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
    }
}
