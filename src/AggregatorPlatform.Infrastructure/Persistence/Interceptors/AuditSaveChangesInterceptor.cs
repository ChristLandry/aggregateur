using System.Text.Json;
using AggregatorPlatform.Application.Interfaces;
using AggregatorPlatform.Domain.Common;
using AggregatorPlatform.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace AggregatorPlatform.Infrastructure.Persistence.Interceptors;

public class AuditSaveChangesInterceptor : SaveChangesInterceptor
{
    private readonly ICurrentUserService _user;

    public AuditSaveChangesInterceptor(ICurrentUserService user)
    {
        _user = user;
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        if (eventData.Context is null) return base.SavingChangesAsync(eventData, result, cancellationToken);

        var now = DateTime.UtcNow;
        var actor = _user.Username ?? _user.UserId?.ToString() ?? "system";

        foreach (var entry in eventData.Context.ChangeTracker.Entries())
        {
            if (entry.Entity is BaseEntity be)
            {
                switch (entry.State)
                {
                    case EntityState.Added:
                        be.CreatedAt = now;
                        break;
                    case EntityState.Modified:
                        be.UpdatedAt = now;
                        break;
                    case EntityState.Deleted:
                        entry.State = EntityState.Modified;
                        be.IsDeleted = true;
                        be.UpdatedAt = now;
                        break;
                }
            }

            if (entry.Entity is AuditableEntity ae)
            {
                if (entry.State == EntityState.Added) ae.CreatedBy = actor;
                if (entry.State == EntityState.Modified) ae.UpdatedBy = actor;
            }
        }

        // Audit log entries
        var auditEntries = new List<AuditLog>();
        foreach (var entry in eventData.Context.ChangeTracker.Entries()
                     .Where(e => e.Entity is not AuditLog &&
                                 (e.State == EntityState.Added || e.State == EntityState.Modified || e.State == EntityState.Deleted)))
        {
            var action = entry.State switch
            {
                EntityState.Added => "CREATE",
                EntityState.Modified => "UPDATE",
                EntityState.Deleted => "DELETE",
                _ => "UNKNOWN"
            };

            auditEntries.Add(new AuditLog
            {
                EntityType = entry.Entity.GetType().Name,
                EntityId = GetEntityId(entry),
                Action = action,
                OldValues = entry.State == EntityState.Added ? null : Serialize(GetOriginalValues(entry)),
                NewValues = entry.State == EntityState.Deleted ? null : Serialize(GetCurrentValues(entry)),
                PerformedBy = actor,
                PerformedAt = now,
                IpAddress = _user.IpAddress,
                UserAgent = _user.UserAgent
            });
        }

        if (auditEntries.Count > 0 && eventData.Context is AggregatorDbContext db)
        {
            db.AuditLogs.AddRange(auditEntries);
        }

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private static string GetEntityId(EntityEntry entry)
    {
        var key = entry.Metadata.FindPrimaryKey();
        if (key is null) return string.Empty;
        var values = key.Properties.Select(p => entry.Property(p.Name).CurrentValue?.ToString() ?? "");
        return string.Join(",", values);
    }

    private static IDictionary<string, object?> GetOriginalValues(EntityEntry entry)
    {
        return entry.Properties.ToDictionary(p => p.Metadata.Name, p => p.OriginalValue);
    }

    private static IDictionary<string, object?> GetCurrentValues(EntityEntry entry)
    {
        return entry.Properties.ToDictionary(p => p.Metadata.Name, p => p.CurrentValue);
    }

    private static string Serialize(IDictionary<string, object?> values)
    {
        try { return JsonSerializer.Serialize(values, new JsonSerializerOptions { WriteIndented = false }); }
        catch { return "{}"; }
    }
}
