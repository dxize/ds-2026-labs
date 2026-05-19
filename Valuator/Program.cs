using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using StackExchange.Redis;
using Valuator.Infrastructure;
using Valuator.Services;

namespace Valuator;

public class Program
{
    public static void Main( string[] args )
    {
        var builder = WebApplication.CreateBuilder( args );

        builder.Services.AddRazorPages( options =>
        {
            options.Conventions.AuthorizePage( "/Index" );
            options.Conventions.AuthorizePage( "/Summary" );
        } );

        var dataProtectionKeysPath = Path.Combine(
            builder.Environment.ContentRootPath,
            "App_Data",
            "DataProtectionKeys"
        );

        Directory.CreateDirectory( dataProtectionKeysPath );

        builder.Services
            .AddDataProtection()
            .PersistKeysToFileSystem( new DirectoryInfo( dataProtectionKeysPath ) )
            .SetApplicationName( "Valuator" );

        builder.Services
            .AddAuthentication( CookieAuthenticationDefaults.AuthenticationScheme )
            .AddCookie( options =>
            {
                options.LoginPath = "/Login";
                options.AccessDeniedPath = "/Login";
                options.ExpireTimeSpan = TimeSpan.FromHours( 8 );
            } );

        builder.Services.AddAuthorization();
        builder.Services.AddSingleton<UserStore>();

        var redisConnectionString =
            builder.Configuration.GetValue<string>( "Redis:ConnectionString" )
            ?? throw new InvalidOperationException( "Missing Redis:ConnectionString in appsettings.json" );

        var rabbitMqHost =
            builder.Configuration.GetValue<string>( "RabbitMq:HostName" )
            ?? throw new InvalidOperationException( "Missing RabbitMq:HostName in appsettings.json" );

        var rabbitMqPort =
            builder.Configuration.GetValue<int?>( "RabbitMq:Port" ) ?? 5672;

        var rabbitMqUserName =
            builder.Configuration.GetValue<string>( "RabbitMq:UserName" )
            ?? throw new InvalidOperationException( "Missing RabbitMq:UserName in appsettings.json" );

        var rabbitMqPassword =
            builder.Configuration.GetValue<string>( "RabbitMq:Password" )
            ?? throw new InvalidOperationException( "Missing RabbitMq:Password in appsettings.json" );

        var rabbitMqExchange =
            builder.Configuration.GetValue<string>( "RabbitMq:ExchangeName" )
            ?? throw new InvalidOperationException( "Missing RabbitMq:ExchangeName in appsettings.json" );

        var rabbitMqQueue =
            builder.Configuration.GetValue<string>( "RabbitMq:QueueName" )
            ?? throw new InvalidOperationException( "Missing RabbitMq:QueueName in appsettings.json" );

        var rabbitMqEventsExchange =
            builder.Configuration.GetValue<string>( "RabbitMq:EventsExchangeName" )
            ?? throw new InvalidOperationException( "Missing RabbitMq:EventsExchangeName in appsettings.json" );

        var mux = ConnectionMultiplexer.Connect( redisConnectionString );
        builder.Services.AddSingleton<IConnectionMultiplexer>( mux );

        builder.Services.AddSingleton( new RabbitMqOptions
        {
            HostName = rabbitMqHost,
            Port = rabbitMqPort,
            UserName = rabbitMqUserName,
            Password = rabbitMqPassword,
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

        app.UseAuthentication();
        app.UseAuthorization();

        app.MapRazorPages();

        app.Run();
    }
}