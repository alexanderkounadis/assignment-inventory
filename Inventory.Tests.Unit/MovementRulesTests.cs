using Inventory.API.Contracts.Inventory;
using Inventory.API.Services;
using Inventory.Domain.Entities;
using Inventory.Tests.Unit.Helpers;


namespace Inventory.Tests.Unit
{
    public sealed class MovementRulesTests
    {
        [Fact]
        public async Task Sale_Cannot_Oversell()
        {
            var tenantId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
            var (db, conn) = TestDb.CreateDb(tenantId);
            try
            {
                // Seed product and warehouse
                db.Products.Add(new Product { Sku = "SKU-1", Name = "P1", Price = 1m, Active = true });
                db.Warehouses.Add(new Warehouse { Name = "W1", IsActive = true });
                await db.SaveChangesAsync();

                var productId = db.Products.Single().Id;
                var warehouseId = db.Warehouses.Single().Id;

                var http = TestDb.CreateHttp(userId: 1);
                var svc = new InventoryService(db, http);

                // Purchase 5
                await svc.CreateMovementAsync(new CreateMovementRequest("Purchase", productId, 5m, WarehouseId: warehouseId));

                // Sale 10 -> should throw
                await Assert.ThrowsAsync<InvalidOperationException>(() =>
                    svc.CreateMovementAsync(new CreateMovementRequest("Sale", productId, 10m, WarehouseId: warehouseId)));
            }
            finally
            {
                db.Dispose();
                conn.Dispose();
            }
        }

        [Fact]
        public async Task Transfer_Requires_Different_Warehouses_And_Sufficient_Stock()
        {
            var tenantId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
            var (db, conn) = TestDb.CreateDb(tenantId);
            try
            {
                db.Products.Add(new Product { Sku = "SKU-1", Name = "P1", Price = 1m, Active = true });
                db.Warehouses.AddRange(
                    new Warehouse { Name = "W1", IsActive = true },
                    new Warehouse { Name = "W2", IsActive = true }
                );
                await db.SaveChangesAsync();

                var productId = db.Products.Single().Id;
                var w1 = db.Warehouses.First(w => w.Name == "W1").Id;
                var w2 = db.Warehouses.First(w => w.Name == "W2").Id;

                var http = TestDb.CreateHttp(userId: 1);
                var svc = new InventoryService(db, http);

                // Purchase 2 into W1
                await svc.CreateMovementAsync(new CreateMovementRequest("Purchase", productId, 2m, WarehouseId: w1));

                // Transfer 5 -> insufficient
                await Assert.ThrowsAsync<InvalidOperationException>(() =>
                    svc.CreateMovementAsync(new CreateMovementRequest("Transfer", productId, 5m, FromWarehouseId: w1, ToWarehouseId: w2)));

                // Transfer with same from/to
                await Assert.ThrowsAsync<ArgumentException>(() =>
                    svc.CreateMovementAsync(new CreateMovementRequest("Transfer", productId, 1m, FromWarehouseId: w1, ToWarehouseId: w1)));
            }
            finally
            {
                db.Dispose();
                conn.Dispose();
            }
        }
    }
}