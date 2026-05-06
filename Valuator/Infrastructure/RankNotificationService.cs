using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.SignalR;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Valuator.Hubs;

namespace Valuator.Infrastructure;

public class RankNotificationService : BackgroundService
{
    private readonly RabbitMqOptions _options;
    private readonly IHubContext<RankHub> _hubContext;

    public RankNotificationService(RabbitMqOptions options, IHubContext<RankHub> hubContext)
    {
        _options = options;
        _hubContext = hubContext;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        ConnectionFactory factory = new()
        {
            HostName = _options.HostName
        };

        await using IConnection connection = await factory.CreateConnectionAsync(stoppingToken);
        await using IChannel channel = await connection.CreateChannelAsync(null, stoppingToken);

        await channel.ExchangeDeclareAsync(
            exchange: _options.EventsExchangeName,
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
            exchange: _options.EventsExchangeName,
            routingKey: "metrics.rank.calculated",
            cancellationToken: stoppingToken
        );

        Console.WriteLine($"RankNotificationService started. Waiting for RankCalculated events...");

        AsyncEventingBasicConsumer consumer = new(channel);
        consumer.ReceivedAsync += async (_, eventArgs) =>
        {
            string json = Encoding.UTF8.GetString(eventArgs.Body.ToArray());
            MetricsEventMessage? message = JsonSerializer.Deserialize<MetricsEventMessage>(json);

            if (message is not null && message.EventType == "RankCalculated")
            {
                await _hubContext.Clients.Group(message.TextId).SendAsync(
                    "RankReady",
                    message.Rank,
                    stoppingToken
                );
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
