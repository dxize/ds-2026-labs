using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace Valuator.Pages;

public class SummaryModel : PageModel
{
    private readonly ILogger<SummaryModel> _logger;
    private readonly IDatabase _db;

    public SummaryModel(ILogger<SummaryModel> logger, IConnectionMultiplexer redis)
    {
        _logger = logger;
        _db = redis.GetDatabase();
    }

    public double Rank { get; set; }
    public double Similarity { get; set; }

    public void OnGet(string id)
    {
        _logger.LogDebug(id);

        if (string.IsNullOrWhiteSpace(id))
        {
            Rank = 0.0;
            Similarity = 0.0;
            return;
        }

        string rankKey = "RANK-" + id;
        string similarityKey = "SIMILARITY-" + id;

        RedisValue rankRaw = _db.StringGet(rankKey);
        Rank = rankRaw.IsNull ? 0.0 : (double)rankRaw;

        RedisValue simRaw = _db.StringGet(similarityKey);
        Similarity = simRaw.IsNull ? 0.0 : (int)simRaw;
    }
}
