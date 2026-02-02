using Inventory.API.Contracts.Auditing;
using Inventory.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Inventory.API.Controllers
{
    [ApiController]
    [Route("api/audit")]
    [Authorize(Roles = "Admin")]
    public sealed class AuditController : ControllerBase
    {
        private readonly InventoryDbContext _db;

        public AuditController(InventoryDbContext db) => _db = db;

        // GET /api/audit?take=50&skip=0&entityType=Product&userId=1
        [HttpGet]
        public async Task<ActionResult<IEnumerable<AuditLogResponse>>> Get(
            [FromQuery] int take = 50,
            [FromQuery] int skip = 0,
            [FromQuery] string? entityType = null,
            [FromQuery] int? userId = null)
        {
            take = Math.Clamp(take, 1, 200);
            skip = Math.Max(0, skip);

            var q = _db.AuditLogs.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(entityType))
            {
                var et = entityType.Trim();
                q = q.Where(a => a.EntityType == et);
            }

            if (userId.HasValue)
            {
                q = q.Where(a => a.UserId == userId.Value);
            }

            var all = await q
                .Select(a => new AuditLogResponse(
                   a.Id,
                   a.UserId,
                   a.EntityType,
                   a.EntityId,
                   a.Operation.ToString(),
                   a.Timestamp,
                   a.BeforeJson,
                   a.AfterJson
                ))
                .ToListAsync();

            var rows = all
                // datetime sorting in .NET is fine - put it here to avoid SQL translation issues
                .OrderByDescending(a => a.Timestamp)  
                .Skip(skip)
                .Take(take)
                .ToList();

            return Ok(rows);
        }
    }
}
