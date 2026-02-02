using Inventory.Domain.Abstractions;
using Inventory.Domain.Entities;
using Inventory.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace Inventory.API.Background
{
    public sealed class LowStockJobOptions
    {
        // default: daily
        public int IntervalSeconds { get; set; } = 86400;

        // default stock threshold
        public decimal Threshold { get; set; } = 5m;      
    }

    public sealed class LowStockReportJob : BackgroundService
    {
        private readonly IServiceProvider _sp;
        private readonly ILogger<LowStockReportJob> _logger;
        private readonly LowStockJobOptions _opt;

        public LowStockReportJob(IServiceProvider sp, ILogger<LowStockReportJob> logger, IOptions<LowStockJobOptions> options)
        {
            _sp = sp;
            _logger = logger;
            _opt = options.Value;
        }
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Small initial delay can help avoid race with app startup/migrations
            await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await GenerateReportsForAllTenants(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Low-stock report job failed.");
                }

                var seconds = Math.Clamp(_opt.IntervalSeconds, 5, 7 * 24 * 3600); // 5s .. 7 days
                await Task.Delay(TimeSpan.FromSeconds(seconds), stoppingToken);
            }
        }

        private async Task GenerateReportsForAllTenants(CancellationToken ct)
        {
            using var scope = _sp.CreateScope();

            // Use the DI context only to discover tenant ids (ignoring filters)
            var db_initial = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();

            var tenantIds = await db_initial.Users
                .IgnoreQueryFilters()
                .Select(u => u.TenantId)
                .Distinct()
                .ToListAsync(ct);

            if (tenantIds.Count == 0)
            {
                _logger.LogInformation("No tenants found for low-stock job.");
                return;
            }

            var options = scope.ServiceProvider.GetRequiredService<DbContextOptions<InventoryDbContext>>();

            foreach (var tenantId in tenantIds)
            {
                await using var db = new InventoryDbContext(options, new FixedTenantProvider(tenantId));

                var rows = await db.InventoryItems
                    .AsNoTracking()
                    .Where(i => i.QuantityOnHand <= _opt.Threshold)
                    .Select(i => new
                    {
                        i.ProductId,
                        Sku = i.Product.Sku,
                        ProductName = i.Product.Name,
                        i.WarehouseId,
                        WarehouseName = i.Warehouse.Name,
                        i.QuantityOnHand
                    })
                    .ToListAsync(ct);

                var report = new LowStockReport
                {
                    TenantId = tenantId,
                    Threshold = _opt.Threshold,
                    GeneratedAt = DateTimeOffset.UtcNow,
                    ReportJson = JsonSerializer.Serialize(rows)
                };

                db.LowStockReports.Add(report);
                await db.SaveChangesAsync(ct);

                _logger.LogInformation(
                    "Low-stock report generated for tenant {TenantId} (Threshold={Threshold}, Rows={Count}).",
                    tenantId, _opt.Threshold, rows.Count);
            }
        }

        private sealed class FixedTenantProvider : ITenantProvider
        {
            public Guid TenantId { get; }
            public bool HasTenant => true;
            public FixedTenantProvider(Guid tenantId) => TenantId = tenantId;
        }
    }
}
