using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

var builder = Host.CreateApplicationBuilder(args);

var rabbitMqHost =
    builder.Configuration.GetValue<string>("RabbitMq:HostName")
    ?? throw new InvalidOperationException("Missing RabbitMq:HostName");

var rabbitMqEventsExchange =
    builder.Configuration.GetValue<string>("RabbitMq:EventsExchangeName")
    ?? throw new InvalidOperationException("Missing RabbitMq:EventsExchangeName");

builder.Services.AddHostedService(_ =>
    new Worker(rabbitMqHost, rabbitMqEventsExchange));

await builder.Build().RunAsync();

public sealed class Worker : BackgroundService
{
    private readonly string _host;
    private readonly string _eventsExchange;

    public Worker(string host, string eventsExchange)
    {
        _host = host;
        _eventsExchange = eventsExchange;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        ConnectionFactory factory = new()
        {
            HostName = _host
        };

        await using IConnection connection = await factory.CreateConnectionAsync(stoppingToken);
        await using IChannel channel = await connection.CreateChannelAsync(null, stoppingToken);

        await channel.ExchangeDeclareAsync(
            exchange: _eventsExchange,
            type: ExchangeType.Topic,
            durable: true,
            autoDelete: false,
            cancellationToken: stoppingToken
        );

        QueueDeclareOk queueInfo = await channel.QueueDeclareAsync(
            queue: string.Empty,
            durable: false,
            exclusive: true,
            autoDelete: true,
            cancellationToken: stoppingToken
        );

        string queueName = queueInfo.QueueName;

        await channel.QueueBindAsync(
            queue: queueName,
            exchange: _eventsExchange,
            routingKey: "metrics.rank.calculated",
            cancellationToken: stoppingToken
        );

        await channel.QueueBindAsync(
            queue: queueName,
            exchange: _eventsExchange,
            routingKey: "metrics.similarity.calculated",
            cancellationToken: stoppingToken
        );

        Console.WriteLine($"EventsLogger started. Queue = {queueName}");

        AsyncEventingBasicConsumer consumer = new(channel);
        consumer.ReceivedAsync += async (_, eventArgs) =>
        {
            string json = Encoding.UTF8.GetString(eventArgs.Body.ToArray());

            MetricsEventMessage? message = JsonSerializer.Deserialize<MetricsEventMessage>(json);

            if (message is null)
            {
                Console.WriteLine($"Unknown event payload: {json}");
            }
            else if (message.EventType == "RankCalculated")
            {
                Console.WriteLine(
                    $"EventType={message.EventType}; TextId={message.TextId}; Rank={message.Rank}; OccurredAtUtc={message.OccurredAtUtc:O}");
            }
            else if (message.EventType == "SimilarityCalculated")
            {
                Console.WriteLine(
                    $"EventType={message.EventType}; TextId={message.TextId}; Similarity={message.Similarity}; OccurredAtUtc={message.OccurredAtUtc:O}");
            }
            else
            {
                Console.WriteLine($"EventType={message.EventType}; TextId={message.TextId}; Payload={json}");
            }

            await channel.BasicAckAsync(eventArgs.DeliveryTag, false);
        };

        await channel.BasicConsumeAsync(
            queue: queueName,
            autoAck: false,
            consumer: consumer,
            cancellationToken: stoppingToken
        );

        await Task.Delay(Timeout.Infinite, stoppingToken);
    }
}

public sealed class MetricsEventMessage
{
    public string EventType { get; init; } = string.Empty;
    public string TextId { get; init; } = string.Empty;
    public double? Rank { get; init; }
    public int? Similarity { get; init; }
    public DateTime OccurredAtUtc { get; init; }
}