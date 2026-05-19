namespace Valuator.Infrastructure;

public sealed class MetricsEventMessage
{
    public string EventType { get; init; } = string.Empty;
    public string TextId { get; init; } = string.Empty;
    public double? Rank { get; init; }
    public int? Similarity { get; init; }
    public DateTime OccurredAtUtc { get; init; }
}