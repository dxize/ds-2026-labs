using Valuator.Infrastructure;

namespace Valuator;

public class Program
{
    public static void Main( string[] args )
    {
        var builder = WebApplication.CreateBuilder( args );

        builder.Services.AddRazorPages();

        var rabbitMqHost =
            builder.Configuration.GetValue<string>( "RabbitMq:HostName" )
            ?? throw new InvalidOperationException( "Missing RabbitMq:HostName in appsettings.json" );

        var rabbitMqExchange =
            builder.Configuration.GetValue<string>( "RabbitMq:ExchangeName" )
            ?? throw new InvalidOperationException( "Missing RabbitMq:ExchangeName in appsettings.json" );

        var rabbitMqQueue =
            builder.Configuration.GetValue<string>( "RabbitMq:QueueName" )
            ?? throw new InvalidOperationException( "Missing RabbitMq:QueueName in appsettings.json" );

        var rabbitMqEventsExchange =
            builder.Configuration.GetValue<string>( "RabbitMq:EventsExchangeName" )
            ?? throw new InvalidOperationException( "Missing RabbitMq:EventsExchangeName in appsettings.json" );

        builder.Services.AddSingleton<RedisShardRouter>();

        builder.Services.AddSingleton( new RabbitMqOptions
        {
            HostName = rabbitMqHost,
            ExchangeName = rabbitMqExchange,
            QueueName = rabbitMqQueue,
            EventsExchangeName = rabbitMqEventsExchange
        } );

        builder.Services.AddSingleton<RankTaskPublisher>();
        builder.Services.AddSingleton<MetricsEventPublisher>();

        var app = builder.Build();

        if ( !app.Environment.IsDevelopment() )
        {
            app.UseExceptionHandler( "/Error" );
        }

        app.UseStaticFiles();
        app.UseRouting();
        app.UseAuthorization();
        app.MapRazorPages();
        app.Run();
    }
}