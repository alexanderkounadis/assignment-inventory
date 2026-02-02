using System.Net.Http.Json;
using Inventory.API.Seed;
using Inventory.Domain.Entities;
using Inventory.Infrastructure;
using Inventory.Tests.Integration.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Inventory.Tests.Integration;

public sealed class AuditLoggingTests : IClassFixture<CustomWebAppFactory>
{
    private readonly CustomWebAppFactory _factory;
    private readonly HttpClient _client;

    public AuditLoggingTests(CustomWebAppFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Creating_Product_Writes_AuditLog()
    {
        var tenant = DbSeeder.TenantA;

        var managerToken = await Login(tenant, "manager@demo.com", "Pass123!");

        // Create a product
        var create = new { sku = "AUDIT-TEST-001", name = "Audit Product", price = 1.11m, active = true };

        using (var req = new HttpRequestMessage(HttpMethod.Post, "/api/products"))
        {
            req.Headers.Add("X-Tenant-Id", tenant.ToString());
            req.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", managerToken);
            req.Content = JsonContent.Create(create);

            var res = await _client.SendAsync(req);
            res.EnsureSuccessStatusCode();
        }

        // Assert audit log row exists for Product Insert in that tenant
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();

        var exists = await db.AuditLogs
            .IgnoreQueryFilters()
            .AnyAsync(a =>
                a.TenantId == tenant &&
                a.EntityType == nameof(Product) &&
                a.Operation == AuditOperation.Insert);

        Assert.True(exists);
    }

    private async Task<string> Login(Guid tenant, string email, string password)
    {
        using var req = new HttpRequestMessage(HttpMethod.Post, "/api/auth/login");
        req.Headers.Add("X-Tenant-Id", tenant.ToString());
        req.Content = JsonContent.Create(new { email, password });

        var res = await _client.SendAsync(req);
        res.EnsureSuccessStatusCode();

        var payload = await res.Content.ReadFromJsonAsync<LoginResponse>();
        return payload!.accessToken;
    }

    private sealed record LoginResponse(string accessToken);
}
