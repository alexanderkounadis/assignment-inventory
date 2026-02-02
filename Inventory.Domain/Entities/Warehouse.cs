using Inventory.Domain.Abstractions;

namespace Inventory.Domain.Entities
{
    public class Warehouse : ITenantEntity
    {
        public int Id { get; set; }
        public Guid TenantId { get; set; }
        public string Name { get; set; } = null!;
        public bool IsActive { get; set; } = true;
    }
}
