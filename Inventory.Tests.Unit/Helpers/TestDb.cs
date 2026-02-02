using Inventory.Domain.Abstractions;
using Inventory.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Inventory.Tests.Unit.Helpers
{
    public static class TestDb
    {
        public static (InventoryDbContext db, SqliteConnection conn) CreateDb(Guid tenantId)
        {
            var conn = new SqliteConnection("Data Source=:memory:");
            conn.Open();

            var options = new DbContextOptionsBuilder<InventoryDbContext>()
                .UseSqlite(conn)
                .Options;

            var db = new InventoryDbContext(options, new FixedTenantProvider(tenantId));
            db.Database.EnsureCreated();

            return (db, conn);
        }

        public static IHttpContextAccessor CreateHttp(int userId)
        {
            var ctx = new DefaultHttpContext();
            ctx.User = new ClaimsPrincipal(
                new ClaimsIdentity(new[]
                {
                new Claim("sub", userId.ToString()),
                new Claim(ClaimTypes.Role, "Manager")
                }, "Test"));

            return new HttpContextAccessor { HttpContext = ctx };
        }

        private sealed class FixedTenantProvider : ITenantProvider
        {
            public Guid TenantId { get; }
            public bool HasTenant => true;
            public FixedTenantProvider(Guid tenantId) => TenantId = tenantId;
        }
    }
}
