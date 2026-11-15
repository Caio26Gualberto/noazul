namespace NoAzul.Application.Interfaces.Pluggy
{
    public sealed record PluggyItem(
        string Id,
        string Status,
        string? ExecutionStatus,
        string? ConnectorName,
        string? ConnectorImageUrl,
        DateTime? LastUpdatedAt);
}
