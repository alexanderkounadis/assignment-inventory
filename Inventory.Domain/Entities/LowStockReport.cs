using Inventory.Domain.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Inventory.Domain.Entities
{
    public sealed class LowStockReport : ITenantEntity
    {
        public long Id { get; set; }
        public Guid TenantId { get; set; }

        public DateTimeOffset GeneratedAt { get; set; } = DateTimeOffset.UtcNow;

        public decimal Threshold { get; set; }
        public string ReportJson { get; set; } = null!;
    }
}
