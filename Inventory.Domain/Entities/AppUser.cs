
using Inventory.Domain.Abstractions;
using static Inventory.Domain.Utilities.Enums;

namespace Inventory.Domain.Entities
{
    public class AppUser : ITenantEntity
    {
        public int Id { get; set; }
        public Guid TenantId { get; set; }
        public string Email { get; set; } = null!;
        public string PasswordHash { get; set; } = null!;
        public AppRole Role { get; set; }
        public bool IsActive { get; set; } = true;
    }
}
