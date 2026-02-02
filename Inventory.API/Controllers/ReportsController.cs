using Inventory.API.Contracts.Reports;
using Inventory.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Inventory.API.Controllers
{
    [ApiController]
    [Route("api/reports")]
    public sealed class ReportsController : ControllerBase
    {
        private readonly InventoryDbContext _db;

        public ReportsController(InventoryDbContext db) => _db = db;

        // GET /api/reports/low-stock
        // Returns latest report for current tenant
        [HttpGet("low-stock")]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<ActionResult<LowStockReportResponse>> GetLatestLowStockReport()
        {
            var report = await _db.LowStockReports
                .AsNoTracking()
                .OrderByDescending(r => r.GeneratedAt)
                .Select(r => new LowStockReportResponse(r.Id, r.GeneratedAt, r.Threshold, r.ReportJson))
                .FirstOrDefaultAsync();

            return report is null ? NotFound(new { error = "No low-stock report found for this tenant yet." }) : Ok(report);
        }

        // Optional: GET /api/reports/low-stock/history?take=10
        [HttpGet("low-stock/history")]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<ActionResult<IEnumerable<LowStockReportResponse>>> GetLowStockHistory([FromQuery] int take = 10)
        {
            take = Math.Clamp(take, 1, 50);

            var reports = await _db.LowStockReports
                .AsNoTracking()
                .OrderByDescending(r => r.GeneratedAt)
                .Take(take)
                .Select(r => new LowStockReportResponse(r.Id, r.GeneratedAt, r.Threshold, r.ReportJson))
                .ToListAsync();

            return Ok(reports);
        }
    }
}
