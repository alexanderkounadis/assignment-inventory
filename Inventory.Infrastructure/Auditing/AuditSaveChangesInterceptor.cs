using Inventory.Domain.Abstractions;
using Inventory.Domain.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System.Security.Claims;
using System.Text.Json;

namespace Inventory.Infrastructure.Auditing;

public sealed class AuditSaveChangesInterceptor : SaveChangesInterceptor
{
    private readonly IHttpContextAccessor _http;

    public AuditSaveChangesInterceptor(IHttpContextAccessor http) => _http = http;

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        AddAuditRows(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        AddAuditRows(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void AddAuditRows(DbContext? context)
    {
        if (context is null) return;

        // materialize entries BEFORE adding AuditLogs (avoid modifying while enumerating)
        var entries = context.ChangeTracker.Entries()
            .Where(e =>
                e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted &&
                e.Entity is not AuditLog)
            .ToList();

        if (entries.Count == 0) return;

        var userId = ResolveUserId();
        var now = DateTimeOffset.UtcNow;

        var auditRows = new List<AuditLog>(entries.Count);

        foreach (var e in entries)
        {
            var tenantId = (e.Entity as ITenantEntity)?.TenantId ?? Guid.Empty;

            var op = e.State switch
            {
                EntityState.Added => AuditOperation.Insert,
                EntityState.Modified => AuditOperation.Update,
                EntityState.Deleted => AuditOperation.Delete,
                _ => throw new ArgumentOutOfRangeException()
            };

            var audit = new AuditLog
            {
                TenantId = tenantId,
                UserId = userId,
                EntityType = e.Metadata.ClrType.Name,
                EntityId = GetPrimaryKeyString(e),
                Operation = op,
                Timestamp = now,
                BeforeJson = op == AuditOperation.Insert ? null : SerializeValues(e.OriginalValues),
                AfterJson = op == AuditOperation.Delete ? null : SerializeValues(e.CurrentValues),
            };

            auditRows.Add(audit);
        }

        // Add AFTER enumeration
        context.Set<AuditLog>().AddRange(auditRows);
    }

    private int ResolveUserId()
    {
        var user = _http.HttpContext?.User;

        var sub =
            user?.FindFirst("sub")?.Value
            ?? user?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        return int.TryParse(sub, out var id) ? id : 0;
    }

    private static string GetPrimaryKeyString(EntityEntry entry)
    {
        var pk = entry.Metadata.FindPrimaryKey();
        if (pk is null) return "";

        var parts = pk.Properties.Select(p =>
        {
            var prop = entry.Property(p.Name);
            var val = prop.CurrentValue ?? prop.OriginalValue;
            return $"{p.Name}={val}";
        });

        return string.Join(",", parts);
    }

    private static string SerializeValues(PropertyValues values)
    {
        var dict = values.Properties.ToDictionary(p => p.Name, p => values[p.Name]);
        return JsonSerializer.Serialize(dict);
    }
}
