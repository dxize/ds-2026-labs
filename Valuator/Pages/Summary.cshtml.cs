using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
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
    public bool RankReady { get; set; }

    public IActionResult OnGet(string id)
    {
        _logger.LogDebug(id);

        if (string.IsNullOrWhiteSpace(id))
        {
            return NotFound();
        }

        string? login = User.Identity?.Name;

        if (string.IsNullOrWhiteSpace(login))
        {
            return Challenge();
        }

        RedisValue author = _db.StringGet("AUTHOR-" + id);

        if (author.IsNullOrEmpty)
        {
            return NotFound();
        }

        if (!string.Equals(author.ToString(), login, StringComparison.OrdinalIgnoreCase))
        {
            return Forbid();
        }

        string rankKey = "RANK-" + id;
        string similarityKey = "SIMILARITY-" + id;

        RedisValue rankRaw = _db.StringGet(rankKey);
        RankReady = !rankRaw.IsNull;
        Rank = rankRaw.IsNull ? 0.0 : (double)rankRaw;

        RedisValue simRaw = _db.StringGet(similarityKey);
        Similarity = simRaw.IsNull ? 0.0 : (int)simRaw;

        return Page();
    }
}
