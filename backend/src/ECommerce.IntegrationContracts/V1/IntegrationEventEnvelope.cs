namespace ECommerce.IntegrationContracts.V1;

public static class IntegrationSchemaVersions
{
    public const string V1 = "1.0";
}

/// <summary>
/// Standard event envelope shared by all plugins for brokered integration events.
/// Payload should contain only integration data, never plugin-private entities.
/// </summary>
public sealed record IntegrationEventEnvelope<TPayload>
    where TPayload : class
{
    public required string EventName { get; init; }
    public required string SchemaVersion { get; init; }
    public required Guid EventId { get; init; }
    public required DateTimeOffset OccurredAtUtc { get; init; }
    public required string CorrelationId { get; init; }
    public string? CausationId { get; init; }
    public required EventResourceIds ResourceIds { get; init; }
    public required TPayload Payload { get; init; }
}

/// <summary>
/// Stable cross-plugin identifiers attached to every message.
/// Null means that ID is not applicable for the specific event.
/// </summary>
public sealed record EventResourceIds
{
    public string? ProductId { get; init; }
    public string? CartId { get; init; }
    public string? UserId { get; init; }
    public string? OrderId { get; init; }
}
