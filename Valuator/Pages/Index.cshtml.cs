using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using StackExchange.Redis;
using Valuator.Infrastructure;

namespace Valuator.Pages;

public class IndexModel : PageModel
{
    private readonly ILogger<IndexModel> _logger;
    private readonly IDatabase _db;
    private readonly RankTaskPublisher _publisher;
    private readonly MetricsEventPublisher _metricsEventPublisher;

    public IndexModel(
        ILogger<IndexModel> logger,
        IConnectionMultiplexer redis,
        RankTaskPublisher publisher,
        MetricsEventPublisher metricsEventPublisher)
    {
        _logger = logger;
        _db = redis.GetDatabase();
        _publisher = publisher;
        _metricsEventPublisher = metricsEventPublisher;
    }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return Page();
        }

        string? login = User.Identity?.Name;

        if (string.IsNullOrWhiteSpace(login))
        {
            return Challenge();
        }

        string id = Guid.NewGuid().ToString();

        string textKey = "TEXT-" + id;
        await _db.StringSetAsync(textKey, text);

        string authorKey = "AUTHOR-" + id;
        await _db.StringSetAsync(authorKey, login);

        string similarityKey = "SIMILARITY-" + id;
        const string allTextsKey = "ALL_TEXTS";

        bool added = await _db.SetAddAsync(allTextsKey, text);
        int similarity = added ? 0 : 1;

        await _db.StringSetAsync(similarityKey, similarity);

        await _metricsEventPublisher.PublishSimilarityCalculatedAsync(id, similarity);
        await _publisher.PublishAsync(id);

        return Redirect($"summary?id={id}");
    }
}
