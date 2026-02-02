using Inventory.API.Contracts.Inventory;
using Inventory.Domain.Entities;
using Inventory.Infrastructure;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Inventory.API.Services
{
    public class InventoryService : IInventoryService
    {
        private readonly InventoryDbContext _db;
        private readonly IHttpContextAccessor _http;

        public InventoryService(InventoryDbContext db, IHttpContextAccessor http)
        {
            _db = db;
            _http = http;
        }
        public async Task<MovementResponse> CreateMovementAsync(CreateMovementRequest request, CancellationToken ct = default)
        {
            if (!Enum.TryParse<StockMovementType>(request.Type, ignoreCase: true, out var type))
                throw new ArgumentException("Invalid movement type. Use Purchase, Sale, Adjustment, Transfer.");

            if (request.ProductId <= 0) throw new ArgumentException("Invalid ProductId.");

            // Quantity rules
            if (type is StockMovementType.Purchase or StockMovementType.Sale or StockMovementType.Transfer)
            {
                if (request.Quantity <= 0) throw new ArgumentException("Quantity must be > 0 for Purchase/Sale/Transfer.");
            }
            else
            {
                if (request.Quantity == 0) throw new ArgumentException("Quantity must be non-zero for Adjustment.");
            }

            var userId = GetUserIdOrThrow();

            // Retry loop for optimistic concurrency
            const int maxAttempts = 3;
            for (var attempt = 1; attempt <= maxAttempts; attempt++)
            {
                await using var tx = await _db.Database.BeginTransactionAsync(ct);

                try
                {
                    // Validate product exists (tenant-filtered)
                    var productExists = await _db.Products.AnyAsync(p => p.Id == request.ProductId, ct);
                    if (!productExists) throw new KeyNotFoundException("Product not found.");

                    InventoryItem? itemA = null;
                    InventoryItem? itemB = null;

                    if (type is StockMovementType.Purchase or StockMovementType.Sale or StockMovementType.Adjustment)
                    {
                        if (request.WarehouseId is null || request.WarehouseId <= 0)
                            throw new ArgumentException("WarehouseId is required for Purchase/Sale/Adjustment.");

                        var warehouseExists = await _db.Warehouses.AnyAsync(w => w.Id == request.WarehouseId, ct);
                        if (!warehouseExists) throw new KeyNotFoundException("Warehouse not found.");

                        itemA = await GetOrCreateInventoryItemAsync(request.ProductId, request.WarehouseId.Value, ct);

                        var delta = type switch
                        {
                            StockMovementType.Purchase => request.Quantity,
                            StockMovementType.Sale => -request.Quantity,
                            StockMovementType.Adjustment => request.Quantity,
                            _ => 0
                        };

                        var newQty = itemA.QuantityOnHand + delta;
                        if (newQty < 0)
                            throw new InvalidOperationException("Insufficient stock (oversell/negative stock prevented).");

                        itemA.QuantityOnHand = newQty;
                    }
                    else // Transfer
                    {
                        if (request.FromWarehouseId is null || request.ToWarehouseId is null)
                            throw new ArgumentException("FromWarehouseId and ToWarehouseId are required for Transfer.");

                        if (request.FromWarehouseId == request.ToWarehouseId)
                            throw new ArgumentException("FromWarehouseId and ToWarehouseId must be different.");

                        var fromExists = await _db.Warehouses.AnyAsync(w => w.Id == request.FromWarehouseId, ct);
                        var toExists = await _db.Warehouses.AnyAsync(w => w.Id == request.ToWarehouseId, ct);
                        if (!fromExists || !toExists) throw new KeyNotFoundException("Warehouse not found.");

                        itemA = await GetOrCreateInventoryItemAsync(request.ProductId, request.FromWarehouseId.Value, ct);
                        itemB = await GetOrCreateInventoryItemAsync(request.ProductId, request.ToWarehouseId.Value, ct);

                        // Prevent oversell/overtransfer
                        if (itemA.QuantityOnHand < request.Quantity)
                            throw new InvalidOperationException("Insufficient stock to transfer.");

                        itemA.QuantityOnHand -= request.Quantity;
                        itemB.QuantityOnHand += request.Quantity;
                    }

                    var movement = new StockMovement
                    {
                        Type = type,
                        ProductId = request.ProductId,
                        Quantity = request.Quantity,
                        WarehouseId = request.WarehouseId,
                        FromWarehouseId = request.FromWarehouseId,
                        ToWarehouseId = request.ToWarehouseId,
                        Notes = request.Notes,
                        CreatedByUserId = userId,
                        CreatedAt = DateTimeOffset.UtcNow
                    };

                    _db.StockMovements.Add(movement);

                    await _db.SaveChangesAsync(ct);
                    await tx.CommitAsync(ct);

                    return new MovementResponse(
                        movement.Id,
                        movement.Type.ToString(),
                        movement.ProductId,
                        movement.Quantity,
                        movement.WarehouseId,
                        movement.FromWarehouseId,
                        movement.ToWarehouseId,
                        movement.CreatedByUserId,
                        movement.CreatedAt
                    );
                }
                catch (DbUpdateConcurrencyException) when (attempt < maxAttempts)
                {
                    await tx.RollbackAsync(ct);
                }
                catch
                {
                    await tx.RollbackAsync(ct);
                    throw;
                }

            }
            throw new InvalidOperationException("Concurrency conflict. Please retry.");
        }

        private async Task<InventoryItem> GetOrCreateInventoryItemAsync(int productId, int warehouseId, CancellationToken ct)
        {
            // Track entity so RowVersion concurrency works
            var item = await _db.InventoryItems
                .FirstOrDefaultAsync(i => i.ProductId == productId && i.WarehouseId == warehouseId, ct);

            if (item is not null) return item;

            item = new InventoryItem
            {
                ProductId = productId,
                WarehouseId = warehouseId,
                QuantityOnHand = 0m
            };

            _db.InventoryItems.Add(item);
            // creates row so later updates are consistent
            await _db.SaveChangesAsync(ct); 
            return item;
        }

        private int GetUserIdOrThrow()
        {
            var user = _http.HttpContext?.User;
            var sub = user?.FindFirstValue(ClaimTypes.NameIdentifier)
                   ?? user?.FindFirstValue("sub")
                   ?? user?.FindFirstValue(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub);

            if (string.IsNullOrWhiteSpace(sub) || !int.TryParse(sub, out var userId))
                throw new InvalidOperationException("Missing/invalid user id claim (sub).");

            return userId;
        }
    }
}
