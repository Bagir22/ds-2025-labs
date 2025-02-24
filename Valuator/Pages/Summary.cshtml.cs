using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using Valuator.Repository;

namespace Valuator.Pages;
public class SummaryModel : PageModel
{
    private readonly ILogger<SummaryModel> _logger;
    private readonly IRedisRepository _redis;
    public SummaryModel(ILogger<SummaryModel> logger,  IRedisRepository redis)
    {
        _logger = logger;
        _redis = redis; 
    }

    public double Rank { get; set; }
    public double Similarity { get; set; }

    public void OnGet(string id)
    {
        _logger.LogDebug(id);
        
        Rank = Convert.ToDouble(_redis.Get($"RANK-{id}") ?? "0");
        Similarity = Convert.ToDouble(_redis.Get($"SIMILARITY-{id}") ?? "0");
    }
}
