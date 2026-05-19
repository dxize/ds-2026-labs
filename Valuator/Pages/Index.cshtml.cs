namespace Valuator.Pages;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using StackExchange.Redis;
using Valuator.Infrastructure;
using RedisShardRouter;


public class IndexModel : PageModel
{
    private readonly ILogger<IndexModel> _logger;
    private readonly RedisShardRouter _redis;
    private readonly RankTaskPublisher _publisher;
    private readonly MetricsEventPublisher _metricsEventPublisher;

    public IndexModel(
        ILogger<IndexModel> logger,
        RedisShardRouter redis,
        RankTaskPublisher publisher,
        MetricsEventPublisher metricsEventPublisher )
    {
        _logger = logger;
        _redis = redis;
        _publisher = publisher;
        _metricsEventPublisher = metricsEventPublisher;
    }

    public IReadOnlyList<string> Countries => RedisShardRouter.Countries;

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync( string text, string country )
    {
        if ( string.IsNullOrWhiteSpace( text ) )
        {
            ModelState.AddModelError( string.Empty, "Введите текст" );
            return Page();
        }

        if ( string.IsNullOrWhiteSpace( country ) || !Countries.Contains( country ) )
        {
            ModelState.AddModelError( string.Empty, "Выберите страну" );
            return Page();
        }

        string id = Guid.NewGuid().ToString();
        string shardKey = RedisShardRouter.GetShardKeyByCountry( country );

        IDatabase mainDb = _redis.MainDb;
        IDatabase shardDb = _redis.GetShardDatabase( shardKey );

        await mainDb.StringSetAsync( id, shardKey );
        _logger.LogInformation( "LOOKUP: {TextId}, {ShardKey}", id, shardKey );

        string textKey = "TEXT-" + id;
        string countryKey = "COUNTRY-" + id;
        string similarityKey = "SIMILARITY-" + id;
        const string allTextsKey = "ALL_TEXTS";

        await shardDb.StringSetAsync( textKey, text );
        await shardDb.StringSetAsync( countryKey, country );

        bool added = await shardDb.SetAddAsync( allTextsKey, text );
        int similarity = added ? 0 : 1;

        await shardDb.StringSetAsync( similarityKey, similarity );

        await _metricsEventPublisher.PublishSimilarityCalculatedAsync( id, similarity );
        await _publisher.PublishAsync( id );

        return Redirect( $"summary?id={id}" );
    }
}