using Microsoft.Extensions.Configuration;
using StackExchange.Redis;

namespace RedisShardRouter;

public sealed class RedisShardRouter : IDisposable
{
    private readonly IConnectionMultiplexer _main;
    private readonly Dictionary<string, IConnectionMultiplexer> _shards;

    public RedisShardRouter( IConfiguration configuration )
    {
        string main = GetConnectionString( configuration, "DB_MAIN", "Redis:Main", "127.0.0.1:6000" );
        string ru = GetConnectionString( configuration, "DB_RU", "Redis:RU", "127.0.0.1:6001" );
        string eu = GetConnectionString( configuration, "DB_EU", "Redis:EU", "127.0.0.1:6002" );
        string asia = GetConnectionString( configuration, "DB_ASIA", "Redis:ASIA", "127.0.0.1:6003" );

        _main = ConnectionMultiplexer.Connect( main );

        _shards = new Dictionary<string, IConnectionMultiplexer>( StringComparer.OrdinalIgnoreCase )
        {
            [ "RU" ] = ConnectionMultiplexer.Connect( ru ),
            [ "EU" ] = ConnectionMultiplexer.Connect( eu ),
            [ "ASIA" ] = ConnectionMultiplexer.Connect( asia )
        };
    }

    public IDatabase MainDb => _main.GetDatabase();

    public IDatabase GetShardDatabase( string shardKey )
    {
        if ( !_shards.TryGetValue( shardKey, out IConnectionMultiplexer? redis ) )
        {
            throw new InvalidOperationException( $"Unknown shard key: {shardKey}" );
        }

        return redis.GetDatabase();
    }

    public async Task<string?> LookupShardKeyAsync( string textId )
    {
        RedisValue shardKey = await MainDb.StringGetAsync( textId );
        return shardKey.IsNull ? null : shardKey.ToString();
    }

    public static string GetShardKeyByCountry( string country )
    {
        return country switch
        {
            "Russia" => "RU",
            "France" => "EU",
            "Germany" => "EU",
            "UAE" => "ASIA",
            "India" => "ASIA",
            _ => throw new InvalidOperationException( $"Unknown country: {country}" )
        };
    }

    public static IReadOnlyList<string> Countries { get; } =
    [
        "Russia",
        "France",
        "Germany",
        "UAE",
        "India"
    ];

    private static string GetConnectionString(
        IConfiguration configuration,
        string environmentVariableName,
        string configurationKey,
        string defaultValue )
    {
        return Environment.GetEnvironmentVariable( environmentVariableName )
               ?? configuration.GetValue<string>( configurationKey )
               ?? defaultValue;
    }

    public void Dispose()
    {
        _main.Dispose();

        foreach ( IConnectionMultiplexer redis in _shards.Values )
        {
            redis.Dispose();
        }
    }
}