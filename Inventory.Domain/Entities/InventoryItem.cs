using Inventory.Domain.Abstractions;

namespace Inventory.Domain.Entities
{
    public sealed class InventoryItem : ITenantEntity
    {
        public int Id { get; set; }
        public Guid TenantId { get; set; }

        public int ProductId { get; set; }
        public Product Product { get; set; } = null!;

        public int WarehouseId { get; set; }
        public Warehouse Warehouse { get; set; } = null!;

        public decimal QuantityOnHand { get; set; }   // use decimal for inventory (supports partial quantities)

        public byte[] RowVersion { get; set; } = null!; // concurrency token
    }
}
