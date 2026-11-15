using FitCore.Application.Abstractions;
using FitCore.Domain.Entities;
using FitCore.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FitCore.Infrastructure.Services;

/// <summary>
/// Writes one AuditLog row per call (PRS §4.3, §13.1).
/// Does not start its own transaction; caller owns the unit of work.
/// </summary>
public sealed class AuditLogger : IAuditLogger
{
    private readonly FitCoreDbContext _db;
    private readonly IClock _clock;
    private readonly ICurrentUser _currentUser;

    public AuditLogger(FitCoreDbContext db, IClock clock, ICurrentUser currentUser)
    {
        _db = db;
        _clock = clock;
        _currentUser = currentUser;
    }

    public async Task LogAsync(
        string action,
        string entityType,
        string? entityId,
        string? oldValue,
        string? newValue,
        string? reason,
        CancellationToken ct = default)
    {
        var entry = new AuditLog
        {
            TimestampUtc = _clock.UtcNow,
            UserId = _currentUser.UserId,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            OldValue = oldValue,
            NewValue = newValue,
            Reason = reason
        };
        _db.AuditLogs.Add(entry);
        await _db.SaveChangesAsync(ct);
    }
}
