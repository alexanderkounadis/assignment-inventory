using Inventory.Domain.Abstractions;

namespace Inventory.API.Tenancy
{
    public sealed class TenantProvider : ITenantProvider
    {
        public Guid TenantId { get; private set; }

        public bool HasTenant { get; private set; }

        /// <summary>
        ///     set the tenant for the current request - only once per request (middleware)
        /// </summary>
        /// <param name="tenantId"></param>
        public void SetTenant(Guid tenantId)
        {
            TenantId = tenantId;
            HasTenant = true;
        }
    }
}
