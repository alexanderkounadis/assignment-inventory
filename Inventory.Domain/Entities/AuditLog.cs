using Inventory.Domain.Abstractions;

namespace Inventory.Domain.Entities
{
    public enum AuditOperation { Insert, Update, Delete }
    public sealed class AuditLog : ITenantEntity
    {
        public long Id { get; set; }
        public Guid TenantId { get; set; }

        public int UserId { get; set; }

        public string EntityType { get; set; } = null!;
        public string EntityId { get; set; } = null!;
        public AuditOperation Operation { get; set; }

        public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;

        public string? BeforeJson { get; set; }
        public string? AfterJson { get; set; }
    }
}
