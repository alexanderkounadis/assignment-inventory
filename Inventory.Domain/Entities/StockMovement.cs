
using Inventory.Domain.Abstractions;

namespace Inventory.Domain.Entities
{
    public enum StockMovementType
    {
        Purchase,
        Sale,
        Adjustment,
        Transfer
    }
    public class StockMovement : ITenantEntity
    {
        public long Id { get; set; }
        public Guid TenantId { get; set; }

        public StockMovementType Type { get; set; }

        public int ProductId { get; set; }
        public Product Product { get; set; } = null!;

        // For Purchase/Sale/Adjustment
        public int? WarehouseId { get; set; }
        public Warehouse? Warehouse { get; set; }

        // For Transfer
        public int? FromWarehouseId { get; set; }
        public Warehouse? FromWarehouse { get; set; }

        public int? ToWarehouseId { get; set; }
        public Warehouse? ToWarehouse { get; set; }

        // Positive for Purchase/Sale/Transfer. Adjustment can be +/-
        public decimal Quantity { get; set; }

        public int CreatedByUserId { get; set; }
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

        public string? Notes { get; set; }
    }
}
