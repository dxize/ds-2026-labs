using System.Text;
using System.Text.Json;
using RabbitMQ.Client;

namespace Valuator.Infrastructure;

public class MetricsEventPublisher
{
    private const string SimilarityCalculatedRoutingKey = "metrics.similarity.calculated";

    private readonly RabbitMqOptions _options;

    public MetricsEventPublisher(RabbitMqOptions options)
    {
        _options = options;
    }

    public Task PublishSimilarityCalculatedAsync(string textId, int similarity)
    {
        MetricsEventMessage message = new()
        {
            EventType = "SimilarityCalculated",
            TextId = textId,
            Similarity = similarity,
            OccurredAtUtc = DateTime.UtcNow
        };

        return PublishAsync(SimilarityCalculatedRoutingKey, message);
    }

    private async Task PublishAsync(string routingKey, MetricsEventMessage message)
    {
        ConnectionFactory factory = new()
        {
            HostName = _options.HostName,
            Port = _options.Port,
            UserName = _options.UserName,
            Password = _options.Password
        };

        await using IConnection connection = await factory.CreateConnectionAsync();
        await using IChannel channel = await connection.CreateChannelAsync();

        await channel.ExchangeDeclareAsync(
            exchange: _options.EventsExchangeName,
            type: ExchangeType.Topic,
            durable: true,
            autoDelete: false
        );

        byte[] body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message));

        await channel.BasicPublishAsync(
            exchange: _options.EventsExchangeName,
            routingKey: routingKey,
            mandatory: false,
            body: body
        );
    }
}
