using Inventory.API.Seed;
using Inventory.Infrastructure;
using Inventory.Infrastructure.Auditing;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Inventory.Tests.Integration.TestHost
{
    public sealed class CustomWebAppFactory : WebApplicationFactory<Inventory.API.Program>
    {
        private SqliteConnection? _connection;

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");

            builder.ConfigureServices(services =>
            {
                // Remove existing DbContext registration
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<InventoryDbContext>));
                if (descriptor is not null) services.Remove(descriptor);

                // Use one in-memory SQLite connection for the whole test host
                _connection = new SqliteConnection("Data Source=:memory:;Cache=Shared");
                _connection.Open();
                services.AddHttpContextAccessor();
                services.AddScoped<AuditSaveChangesInterceptor>();
                services.AddDbContext<InventoryDbContext>((sp, opt) =>
                {
                    opt.UseSqlite(_connection);
                    opt.AddInterceptors(sp.GetRequiredService<AuditSaveChangesInterceptor>());
                });

                // OPTIONAL: disable background services to keep DB stable during asserts
                var hostedServices = services.Where(s => s.ServiceType == typeof(IHostedService)).ToList();
                foreach (var hs in hostedServices) services.Remove(hs);

                // Build a provider so we can initialize DB + seed now
                var sp = services.BuildServiceProvider();

                using var scope = sp.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();
                db.Database.Migrate();

                DbSeeder.SeedAsync(sp).GetAwaiter().GetResult();
            });
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            _connection?.Dispose();
        }
    }
}
