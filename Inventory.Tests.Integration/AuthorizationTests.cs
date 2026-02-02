using System.Net;
using System.Net.Http.Json;
using Inventory.API.Seed;
using Inventory.Tests.Integration.TestHost;
using Xunit;

namespace Inventory.Tests.Integration;

public sealed class AuthorizationTests : IClassFixture<CustomWebAppFactory>
{
    private readonly HttpClient _client;

    public AuthorizationTests(CustomWebAppFactory factory) => _client = factory.CreateClient();

    [Fact]
    public async Task Clerk_Cannot_Create_Inventory_Movement_Returns_403()
    {
        var tenant = DbSeeder.TenantA;

        var clerkToken = await Login(tenant, "clerk@demo.com", "Pass123!");

        // Read seeded product/warehouse to use real IDs
        var products = await Get<List<ProductDto>>(tenant, clerkToken, "/api/products");
        var warehouses = await Get<List<WarehouseDto>>(tenant, clerkToken, "/api/warehouses");

        var productId = products![0].id;
        var warehouseId = warehouses![0].id;

        var movement = new
        {
            type = "Purchase",
            productId,
            warehouseId,
            quantity = 1
        };

        using var req = new HttpRequestMessage(HttpMethod.Post, "/api/inventory/movements");
        req.Headers.Add("X-Tenant-Id", tenant.ToString());
        req.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", clerkToken);
        req.Content = JsonContent.Create(movement);

        var res = await _client.SendAsync(req);

        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
    }

    private async Task<string> Login(Guid tenant, string email, string password)
    {
        using var req = new HttpRequestMessage(HttpMethod.Post, "/api/auth/login");
        req.Headers.Add("X-Tenant-Id", tenant.ToString());
        req.Content = JsonContent.Create(new { email, password });

        var res = await _client.SendAsync(req);
        res.EnsureSuccessStatusCode();

        var payload = await res.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.NotNull(payload);
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
}
