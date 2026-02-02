using Inventory.Domain.Abstractions;
using Inventory.Domain.Entities;
using Inventory.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using static Inventory.Domain.Utilities.Enums;

namespace Inventory.API.Seed
{
    public static class DbSeeder
    {
        // demo tenants  you can grab tenant id in README too
        public static readonly Guid TenantA = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        public static readonly Guid TenantB = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

        public static async Task SeedAsync(IServiceProvider root)
        {
  
            // Seed each tenant with a context that has HasTenant=true
            await SeedTenantAsync(root, TenantA);
            await SeedTenantAsync(root, TenantB);
        }

        private static async Task SeedTenantAsync(IServiceProvider root, Guid tenantId)
        {
            using var scope = root.CreateScope();

            var options = scope.ServiceProvider.GetRequiredService<DbContextOptions<InventoryDbContext>>();
            var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<AppUser>>();

            await using var db = new InventoryDbContext(options, new SeedTenantProvider(tenantId));

            // Seed users (idempotent)
            if (!await db.Users.AnyAsync())
            {
                db.Users.AddRange(
                    CreateUser(hasher, tenantId, "admin@demo.com", AppRole.Admin, "Pass123!"),
                    CreateUser(hasher, tenantId, "manager@demo.com", AppRole.Manager, "Pass123!"),
                    CreateUser(hasher, tenantId, "clerk@demo.com", AppRole.Clerk, "Pass123!")
                );

                await db.SaveChangesAsync();
            }

            // Seed warehouses (idempotent)
            if (!await db.Warehouses.AnyAsync())
            {
                db.Warehouses.AddRange(
                    new Warehouse { Name = "Main Warehouse", IsActive = true },
                    new Warehouse { Name = "Secondary Warehouse", IsActive = true }
                );

                await db.SaveChangesAsync();
            }

            // Seed products (idempotent)
            if (!await db.Products.AnyAsync())
            {
                db.Products.AddRange(
                    new Product { Sku = "SKU-COFFEE-001", Name = "Coffee Beans 1kg", Price = 14.99m, Active = true },
                    new Product { Sku = "SKU-TEA-001", Name = "Green Tea 200g", Price = 6.50m, Active = true }
                );

                await db.SaveChangesAsync();
            }
        }

        private static AppUser CreateUser(IPasswordHasher<AppUser> hasher, Guid tenantId, string email, AppRole role, string password)
        {
            var user = new AppUser
            {
                TenantId = tenantId, // explicit for safety
                Email = email.Trim().ToLowerInvariant(),
                Role = role,
                IsActive = true
            };

            user.PasswordHash = hasher.HashPassword(user, password);
            return user;
        }

        private sealed class SeedTenantProvider : ITenantProvider
        {
            public Guid TenantId { get; }
            public bool HasTenant => true;
            public SeedTenantProvider(Guid tenantId) => TenantId = tenantId;
        }
    }
}
