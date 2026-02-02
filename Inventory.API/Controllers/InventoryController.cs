using Inventory.API.Contracts.Inventory;
using Inventory.API.Services;
using Inventory.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Inventory.API.Controllers
{
    [Route("api/inventory")]
    public class InventoryController : ControllerBase
    {
        private readonly InventoryDbContext _db;
        private readonly IInventoryService _svc;

        public InventoryController(InventoryDbContext db, IInventoryService svc)
        {
            _db = db;
            _svc = svc;
        }

        // GET /api/inventory/stock
        [HttpGet("stock")]
        [Authorize(Roles = "Admin,Manager,Clerk")]
        public async Task<ActionResult<IEnumerable<StockRowResponse>>> GetStock()
        {
            var rows = await _db.InventoryItems
                .AsNoTracking()
                .OrderBy(i => i.WarehouseId)
                .ThenBy(i => i.ProductId)
                .Select(i => new StockRowResponse(
                    i.WarehouseId,
                    i.Warehouse.Name,
                    i.ProductId,
                    i.Product.Sku,
                    i.Product.Name,
                    i.QuantityOnHand
                ))
                .ToListAsync();

            return Ok(rows);
        }

        // POST /api/inventory/movements
        [HttpPost("movements")]
        [Authorize(Roles = "Manager")]
        public async Task<ActionResult<MovementResponse>> CreateMovement([FromBody] CreateMovementRequest request, CancellationToken ct)
        {
            try
            {
                var result = await _svc.CreateMovementAsync(request, ct);
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { error = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                // oversell, insufficient stock, or concurrency message
                return Conflict(new { error = ex.Message });
            }
        }
    }
}
