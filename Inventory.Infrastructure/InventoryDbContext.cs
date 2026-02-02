using Inventory.Domain.Abstractions;
using Inventory.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Inventory.Infrastructure
{
    public sealed class InventoryDbContext : DbContext
    {
        private readonly ITenantProvider _tenantProvider;
        public InventoryDbContext(DbContextOptions<InventoryDbContext> options, ITenantProvider tenantProvider)
        : base(options)
        {
            _tenantProvider = tenantProvider;
        }

        public DbSet<Product> Products => Set<Product>();
        public DbSet<AppUser> Users => Set<AppUser>();
        public DbSet<Warehouse> Warehouses => Set<Warehouse>();
        public DbSet<InventoryItem> InventoryItems => Set<InventoryItem>();
        public DbSet<StockMovement> StockMovements => Set<StockMovement>();
        public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
        public DbSet<LowStockReport> LowStockReports => Set<LowStockReport>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // PRODUCT: SKU unique per tenant
            modelBuilder.Entity<Product>()
                .HasIndex(p => new { p.TenantId, p.Sku })
                .IsUnique();

            // USER: Email unique per tenant
            modelBuilder.Entity<AppUser>()
                .HasIndex(u => new { u.TenantId, u.Email })
                .IsUnique();

            // WAREHOUSE: Name unique per tenant
            modelBuilder.Entity<Warehouse>()
                .HasIndex(w => new { w.TenantId, w.Name })
                .IsUnique();

            modelBuilder.Entity<InventoryItem>()
                .HasIndex(i => new { i.TenantId, i.ProductId, i.WarehouseId })
                .IsUnique();

            // EF will throw DbUpdateConcurrencyException when two updates race
            modelBuilder.Entity<InventoryItem>()
                .Property(i => i.RowVersion)
                .IsConcurrencyToken()
                .IsRequired();

            modelBuilder.Entity<InventoryItem>()
                .HasOne(i => i.Product)
                .WithMany()
                .HasForeignKey(i => i.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<InventoryItem>()
                .HasOne(i => i.Warehouse)
                .WithMany()
                .HasForeignKey(i => i.WarehouseId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<StockMovement>()
                .HasQueryFilter(m => m.TenantId == _tenantProvider.TenantId);

            modelBuilder.Entity<StockMovement>()
                .HasOne(m => m.Product)
                .WithMany()
                .HasForeignKey(m => m.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<StockMovement>()
                .HasOne(m => m.Warehouse)
                .WithMany()
                .HasForeignKey(m => m.WarehouseId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<StockMovement>()
                .HasOne(m => m.FromWarehouse)
                .WithMany()
                .HasForeignKey(m => m.FromWarehouseId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<StockMovement>()
                .HasOne(m => m.ToWarehouse)
                .WithMany()
                .HasForeignKey(m => m.ToWarehouseId)
                .OnDelete(DeleteBehavior.Restrict);

            // Tenant isolation (global filters)
            modelBuilder.Entity<Product>()
                .HasQueryFilter(p => p.TenantId == _tenantProvider.TenantId);

            modelBuilder.Entity<AppUser>()
                .HasQueryFilter(u => u.TenantId == _tenantProvider.TenantId);

            modelBuilder.Entity<Warehouse>()
                .HasQueryFilter(w => w.TenantId == _tenantProvider.TenantId);

            modelBuilder.Entity<InventoryItem>()
                .HasQueryFilter(i => i.TenantId == _tenantProvider.TenantId);

            modelBuilder.Entity<AuditLog>()
                .HasQueryFilter(a => a.TenantId == _tenantProvider.TenantId);

            modelBuilder.Entity<LowStockReport>()
                .HasQueryFilter(r => r.TenantId == _tenantProvider.TenantId);

            modelBuilder.Entity<LowStockReport>()
                .Property(r => r.GeneratedAt)
                .HasConversion(new DateTimeOffsetToBinaryConverter());
        }

        public override int SaveChanges()
        {
            ApplyTenantRules();
            return base.SaveChanges();
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            ApplyTenantRules();
            return base.SaveChangesAsync(cancellationToken);
        }

        private void ApplyTenantRules()
        {
            if (!_tenantProvider.HasTenant)
                throw new InvalidOperationException("Tenant is not resolved for this request.");

            var tenantId = _tenantProvider.TenantId;

            foreach (var entry in ChangeTracker.Entries<ITenantEntity>())
            {
                if (entry.State == EntityState.Added)
                {
                    if (entry.Entity.TenantId == Guid.Empty)
                        entry.Entity.TenantId = tenantId;
                    else if (entry.Entity.TenantId != tenantId)
                        throw new InvalidOperationException("Cross-tenant write attempt detected.");

                    // SQLite-friendly concurrency token initialization
                    if (entry.Entity is InventoryItem ii)
                    {
                        ii.RowVersion ??= Guid.NewGuid().ToByteArray();
                    }
                }
                else if (entry.State == EntityState.Modified)
                {
                    if (entry.Entity.TenantId != tenantId)
                        throw new InvalidOperationException("Cross-tenant write attempt detected.");

                    // bump RowVersion on every update to enable optimistic concurrency
                    if (entry.Entity is InventoryItem ii)
                    {
                        ii.RowVersion = Guid.NewGuid().ToByteArray();
                    }
                }
                else if (entry.State == EntityState.Deleted)
                {
                    if (entry.Entity.TenantId != tenantId)
                        throw new InvalidOperationException("Cross-tenant write attempt detected.");
                }
            }
        }
    }
}
