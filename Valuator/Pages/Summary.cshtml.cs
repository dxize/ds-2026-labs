namespace Valuator.Pages;
using Microsoft.AspNetCore.Mvc.RazorPages;
using StackExchange.Redis;
using RedisShardRouter;


public class SummaryModel : PageModel
{
    private readonly ILogger<SummaryModel> _logger;
    private readonly RedisShardRouter _redis;

    public SummaryModel( ILogger<SummaryModel> logger, RedisShardRouter redis )
    {
        _logger = logger;
        _redis = redis;
    }

    public double Rank { get; set; }
    public double Similarity { get; set; }
    public bool RankReady { get; set; }
    public string? Country { get; set; }
    public string? ShardKey { get; set; }
    public bool TextFound { get; set; }

    public async Task OnGetAsync( string id )
    {
        if ( string.IsNullOrWhiteSpace( id ) )
        {
            Rank = 0.0;
            Similarity = 0.0;
            RankReady = false;
            TextFound = false;
            return;
        }

        ShardKey = await _redis.LookupShardKeyAsync( id );

        if ( string.IsNullOrWhiteSpace( ShardKey ) )
        {
            _logger.LogWarning( "LOOKUP: {TextId}, NOT_FOUND", id );
            Rank = 0.0;
            Similarity = 0.0;
            RankReady = false;
            TextFound = false;
            return;
        }

        _logger.LogInformation( "LOOKUP: {TextId}, {ShardKey}", id, ShardKey );

        IDatabase shardDb = _redis.GetShardDatabase( ShardKey );

        string rankKey = "RANK-" + id;
        string similarityKey = "SIMILARITY-" + id;
        string countryKey = "COUNTRY-" + id;

        RedisValue countryRaw = await shardDb.StringGetAsync( countryKey );
        Country = countryRaw.IsNull ? null : countryRaw.ToString();

        RedisValue rankRaw = await shardDb.StringGetAsync( rankKey );
        RankReady = !rankRaw.IsNull;
        Rank = rankRaw.IsNull ? 0.0 : ( double )rankRaw;

        RedisValue simRaw = await shardDb.StringGetAsync( similarityKey );
        Similarity = simRaw.IsNull ? 0.0 : ( int )simRaw;

        TextFound = true;
    }
}