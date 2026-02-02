using System.Net;
using System.Net.Http.Json;
using Inventory.API.Seed;
using Inventory.Tests.Integration.TestHost;
using Xunit;

namespace Inventory.Tests.Integration;

public sealed class InventoryWorkflowTests : IClassFixture<CustomWebAppFactory>
{
    private readonly HttpClient _client;

    public InventoryWorkflowTests(CustomWebAppFactory factory) => _client = factory.CreateClient();

    [Fact]
    public async Task Manager_Purchase_Creates_Stock_Row()
    {
        var tenant = DbSeeder.TenantA;

        var managerToken = await Login(tenant, "manager@demo.com", "Pass123!");

        var products = await Get<List<ProductDto>>(tenant, managerToken, "/api/products");
        var warehouses = await Get<List<WarehouseDto>>(tenant, managerToken, "/api/warehouses");

        var productId = products![0].id;
        var warehouseId = warehouses![0].id;

        // Purchase 10 items
        var movement = new { type = "Purchase", productId, warehouseId, quantity = 10m };

        using (var req = new HttpRequestMessage(HttpMethod.Post, "/api/inventory/movements"))
        {
            req.Headers.Add("X-Tenant-Id", tenant.ToString());
            req.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", managerToken);
            req.Content = JsonContent.Create(movement);

            var res = await _client.SendAsync(req);
            Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        }

        // Verify stock contains that row with qty >= 10
        var stock = await Get<List<StockRow>>(tenant, managerToken, "/api/inventory/stock");

        var row = stock!.FirstOrDefault(s => s.productId == productId && s.warehouseId == warehouseId);
        Assert.NotNull(row);
        Assert.True(row!.quantityOnHand >= 10m);
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

    private async Task<T?> Get<T>(Guid tenant, string token, string url)
    {
        using var req = new HttpRequestMessage(HttpMethod.Get, url);
        req.Headers.Add("X-Tenant-Id", tenant.ToString());
        req.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var res = await _client.SendAsync(req);
        res.EnsureSuccessStatusCode();

        return await res.Content.ReadFromJsonAsync<T>();
    }

    private sealed record LoginResponse(string accessToken);
    private sealed record ProductDto(int id, string sku, string name, decimal price, bool active);
    private sealed record WarehouseDto(int id, string name, bool isActive);
    private sealed record StockRow(int warehouseId, string warehouseName, int productId, string sku, string productName, decimal quantityOnHand);
}
