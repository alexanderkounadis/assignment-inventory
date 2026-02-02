using Inventory.Domain.Abstractions;

namespace Inventory.Domain.Entities
{
    public class Product : ITenantEntity
    {
        public int Id { get; set; }
        public Guid TenantId { get; set; }
        public string Sku { get; set; } = string.Empty!;
        public string Name { get; set; } = string.Empty!;
        public decimal Price { get; set; } = default;
        public bool Active { get; set; } = true;
    }
}
