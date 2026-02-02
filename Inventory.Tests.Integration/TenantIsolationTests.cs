using Inventory.API.Seed;
using Inventory.Tests.Integration.TestHost;
using System.Net.Http.Json;

namespace Inventory.Tests.Integration
{
    public sealed class TenantIsolationTests : IClassFixture<CustomWebAppFactory>
    {
        private readonly HttpClient _client;

        public TenantIsolationTests(CustomWebAppFactory factory)
        {
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task Products_Are_Isolated_By_Tenant_Header()
        {
            // Tenant A login as Manager
            var tokenA = await LoginAndGetToken(DbSeeder.TenantA, "manager@demo.com", "Pass123!");
            // Tenant B login as Manager
            var tokenB = await LoginAndGetToken(DbSeeder.TenantB, "manager@demo.com", "Pass123!");

            // Create product in Tenant A
            var createdA = await CreateProduct(DbSeeder.TenantA, tokenA, new
            {
                sku = "TEST-A-001",
                name = "TenantA Product",
                price = 1.23,
                active = true
            });

            // Create product in Tenant B with same SKU (allowed across tenants)
            var createdB = await CreateProduct(DbSeeder.TenantB, tokenB, new
            {
                sku = "TEST-A-001",
                name = "TenantB Product",
                price = 9.99,
                active = true
            });

            // Fetch products under Tenant A
            var listA = await GetProducts(DbSeeder.TenantA, tokenA);
            Assert.Contains(listA, p => p.sku == "TEST-A-001" && p.name == "TenantA Product");

            // Fetch products under Tenant B
            var listB = await GetProducts(DbSeeder.TenantB, tokenB);
            Assert.Contains(listB, p => p.sku == "TEST-A-001" && p.name == "TenantB Product");

            // Isolation: Tenant A should not see Tenant B product name
            Assert.DoesNotContain(listA, p => p.name == "TenantB Product");
            Assert.DoesNotContain(listB, p => p.name == "TenantA Product");
        }

        private async Task<string> LoginAndGetToken(Guid tenantId, string email, string password)
        {
            using var req = new HttpRequestMessage(HttpMethod.Post, "/api/auth/login");
            req.Headers.Add("X-Tenant-Id", tenantId.ToString());
            req.Content = JsonContent.Create(new { email, password });

            var res = await _client.SendAsync(req);
            res.EnsureSuccessStatusCode();

            var payload = await res.Content.ReadFromJsonAsync<LoginResponse>();
            Assert.NotNull(payload);
            Assert.False(string.IsNullOrWhiteSpace(payload!.accessToken));
            return payload.accessToken;
        }

        private async Task<ProductResponse> CreateProduct(Guid tenantId, string token, object body)
        {
            using var req = new HttpRequestMessage(HttpMethod.Post, "/api/products");
            req.Headers.Add("X-Tenant-Id", tenantId.ToString());
            req.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            req.Content = JsonContent.Create(body);

            var res = await _client.SendAsync(req);
            res.EnsureSuccessStatusCode();

            var created = await res.Content.ReadFromJsonAsync<ProductResponse>();
            Assert.NotNull(created);
            return created!;
        }

        private async Task<List<ProductResponse>> GetProducts(Guid tenantId, string token)
        {
            using var req = new HttpRequestMessage(HttpMethod.Get, "/api/products");
            req.Headers.Add("X-Tenant-Id", tenantId.ToString());
            req.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var res = await _client.SendAsync(req);
            res.EnsureSuccessStatusCode();

            var list = await res.Content.ReadFromJsonAsync<List<ProductResponse>>();
            return list ?? new List<ProductResponse>();
        }

        private sealed record LoginResponse(string accessToken);
        private sealed record ProductResponse(int id, string sku, string name, decimal price, bool active);
    }
}
