namespace Inventory.API.Contracts.Auditing
{
    public sealed record AuditLogResponse(
    long Id,
    int UserId,
    string EntityType,
    string EntityId,
    string Operation,
    DateTimeOffset Timestamp,
    string? BeforeJson,
    string? AfterJson
);
}
