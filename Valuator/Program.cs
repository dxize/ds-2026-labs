using StackExchange.Redis;

namespace Valuator;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.
        builder.Services.AddRazorPages();

        // 1) Читаем строку подключения из appsettings: Redis:ConnectionString
        var redisConnectionString =
            builder.Configuration.GetValue<string>("Redis:ConnectionString")
            ?? throw new InvalidOperationException("Missing Redis:ConnectionString in appsettings.json");

        // 2) Регистрируем Redis multiplexer как Singleton
        builder.Services.AddSingleton<IConnectionMultiplexer>(_ =>
            ConnectionMultiplexer.Connect(redisConnectionString));

        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Error");
        }
        app.UseStaticFiles();

        app.UseRouting();

        app.UseAuthorization();

        app.MapRazorPages();

        app.Run();
    }
}
