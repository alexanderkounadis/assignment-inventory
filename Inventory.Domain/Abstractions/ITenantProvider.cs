namespace Inventory.Domain.Abstractions
{
    // <summary>
    //      read-only interface not able to set the tenant id
    // </summary>
    public interface ITenantProvider
    {
        Guid TenantId { get; }
        bool HasTenant { get; }
    }
}
