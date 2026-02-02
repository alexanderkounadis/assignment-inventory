using Inventory.API.Tenancy;
using System.Net;

namespace Inventory.API;

public sealed class TenantResolutionMiddleware : IMiddleware
{
    private const string HeaderName = "X-Tenant-Id";
    private readonly TenantProvider _tenantProvider;

    public TenantResolutionMiddleware(TenantProvider tenantProvider)
    {
        _tenantProvider = tenantProvider;
    }

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        var path = context.Request.Path;

        // Allow Swagger/OpenAPI endpoints without tenant header
        if (path.StartsWithSegments("/swagger", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWithSegments("/openapi", StringComparison.OrdinalIgnoreCase))
        {
            await next(context);
            return;
        }

        var raw = context.Request.Headers[HeaderName].ToString();

        if (!Guid.TryParse(raw, out var tenantId) || tenantId == Guid.Empty)
        {
            context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
            await context.Response.WriteAsJsonAsync(new { error = "Missing or invalid X-Tenant-Id header." });
            return;
        }

        _tenantProvider.SetTenant(tenantId);
        await next(context);
    }
}
