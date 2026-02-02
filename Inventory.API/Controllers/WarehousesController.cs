using Inventory.API.Contracts.Warehouses;
using Inventory.Domain.Entities;
using Inventory.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Inventory.API.Controllers
{
    [ApiController]
    [Route("api/warehouses")]
    public class WarehousesController : ControllerBase
    {
        private readonly InventoryDbContext _db;

        public WarehousesController(InventoryDbContext db) => _db = db;

        // GET /api/warehouses
        [HttpGet]
        [Authorize(Roles = "Admin,Manager,Clerk")]
        public async Task<ActionResult<IEnumerable<WarehouseResponse>>> GetAll()
        {
            var warehouses = await _db.Warehouses
                .AsNoTracking()
                .OrderBy(w => w.Id)
                .Select(w => new WarehouseResponse(w.Id, w.Name, w.IsActive))
                .ToListAsync();

            return Ok(warehouses);
        }

        // GET /api/warehouses/{id}
        [HttpGet("{id:int}")]
        [Authorize(Roles = "Admin,Manager,Clerk")]
        public async Task<ActionResult<WarehouseResponse>> GetById(int id)
        {
            var warehouse = await _db.Warehouses
                .AsNoTracking()
                .Where(w => w.Id == id)
                .Select(w => new WarehouseResponse(w.Id, w.Name, w.IsActive))
                .FirstOrDefaultAsync();

            return warehouse is null ? NotFound() : Ok(warehouse);
        }

        // POST /api/warehouses
        [HttpPost]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<ActionResult<WarehouseResponse>> Create([FromBody] CreateWarehouseRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Name))
                return BadRequest(new { error = "Name is required." });

            var entity = new Warehouse
            {
                Name = request.Name.Trim(),
                IsActive = request.IsActive
                // TenantId set by SaveChanges tenant rules
            };

            _db.Warehouses.Add(entity);

            try
            {
                await _db.SaveChangesAsync();
            }
            catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
            {
                return Conflict(new { error = $"Warehouse '{entity.Name}' already exists in this tenant." });
            }

            var response = new WarehouseResponse(entity.Id, entity.Name, entity.IsActive);
            return CreatedAtAction(nameof(GetById), new { id = entity.Id }, response);
        }

        // PUT /api/warehouses/{id}
        [HttpPut("{id:int}")]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<ActionResult<WarehouseResponse>> Update(int id, [FromBody] UpdateWarehouseRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Name))
                return BadRequest(new { error = "Name is required." });

            var entity = await _db.Warehouses.FirstOrDefaultAsync(w => w.Id == id);
            if (entity is null) return NotFound();

            entity.Name = request.Name.Trim();
            entity.IsActive = request.IsActive;

            try
            {
                await _db.SaveChangesAsync();
            }
            catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
            {
                return Conflict(new { error = $"Warehouse '{entity.Name}' already exists in this tenant." });
            }

            return Ok(new WarehouseResponse(entity.Id, entity.Name, entity.IsActive));
        }

        // DELETE /api/warehouses/{id}
        [HttpDelete("{id:int}")]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> Delete(int id)
        {
            var entity = await _db.Warehouses.FirstOrDefaultAsync(w => w.Id == id);
            if (entity is null) return NotFound();

            _db.Warehouses.Remove(entity);
            await _db.SaveChangesAsync();

            return NoContent();
        }

        private static bool IsUniqueConstraintViolation(DbUpdateException ex)
        {
            var msg = ex.InnerException?.Message ?? ex.Message;
            return msg.Contains("UNIQUE", StringComparison.OrdinalIgnoreCase)
                || msg.Contains("unique constraint", StringComparison.OrdinalIgnoreCase);
        }
    }
}
