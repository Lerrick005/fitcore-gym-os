using System.Collections.Concurrent;
using System.Text.Json;
using FitCore.Application.Abstractions;
using FitCore.Application.Common;
using FitCore.Application.Settings;
using FitCore.Domain.Entities;
using FitCore.Infrastructure.Persistence;
using FitCore.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace FitCore.Infrastructure.Settings;

/// <summary>
/// Single path to configuration (PRS §6). Cached, audited, transactional, change-event aware.
/// </summary>
public sealed class SettingsService : ISettingsService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = false,
        WriteIndented = false
    };

    private readonly FitCoreDbContext _db;
    private readonly IClock _clock;
    private readonly ICurrentUser _currentUser;
    private readonly ConcurrentDictionary<string, object?> _cache = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _groupLocks = new(StringComparer.Ordinal);

    public event EventHandler<SettingsChangedEventArgs>? Changed;

    public SettingsService(FitCoreDbContext db, IClock clock, ICurrentUser currentUser)
    {
        _db = db;
        _clock = clock;
        _currentUser = currentUser;
    }

    public async Task<T> GetAsync<T>(CancellationToken ct = default)
        where T : class, ISettingsGroup, new()
    {
        var key = T.GroupKey;
        if (_cache.TryGetValue(key, out var cached) && cached is T typed)
            return typed;

        var row = await _db.SettingsEntries
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.GroupKey == key, ct);

        T value;
        if (row is null)
        {
            value = new T();
        }
        else
        {
            value = JsonSerializer.Deserialize<T>(row.JsonValue, JsonOptions) ?? new T();
        }

        _cache[key] = value;
        return value;
    }

    public async Task<Result> SaveAsync<T>(T value, CancellationToken ct = default)
        where T : class, ISettingsGroup, new()
    {
        if (value is null) return Result.Fail("settings.null", "Settings value is required.");

        var key = T.GroupKey;

        // Permission: bypass only inside the wizard's system context (PRS §10).
        var isSystem = SettingsSystemContext.IsActive;
        if (!isSystem && !_currentUser.HasPermission("settings.manage"))
            return Result.Fail("permission.denied", "You do not have permission to change settings.");

        var gate = _groupLocks.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync(ct);
        try
        {
            var newJson = JsonSerializer.Serialize(value, JsonOptions);

            var existing = await _db.SettingsEntries
                .FirstOrDefaultAsync(x => x.GroupKey == key, ct);

            // No-op save: skip transaction and audit.
            if (existing is not null && string.Equals(existing.JsonValue, newJson, StringComparison.Ordinal))
            {
                _cache[key] = value;
                return Result.Ok();
            }

            var oldJson = existing?.JsonValue; // null if row did not exist
            var now = _clock.UtcNow;
            var userId = isSystem ? (int?)null : _currentUser.UserId;

            await using var tx = await _db.Database.BeginTransactionAsync(ct);
            try
            {
                if (existing is null)
                {
                    _db.SettingsEntries.Add(new SettingsEntry
                    {
                        GroupKey = key,
                        SchemaVersion = 1,
                        JsonValue = newJson,
                        UpdatedAtUtc = now,
                        UpdatedBy = userId
                    });
                }
                else
                {
                    existing.JsonValue = newJson;
                    existing.UpdatedAtUtc = now;
                    existing.UpdatedBy = userId;
                }

                _db.AuditLogs.Add(new AuditLog
                {
                    TimestampUtc = now,
                    UserId = userId,
                    Action = "settings.save",
                    EntityType = "SettingsGroup",
                    EntityId = key,
                    OldValue = oldJson,
                    NewValue = newJson,
                    Reason = isSystem ? "wizard" : null
                });

                await _db.SaveChangesAsync(ct);
                await tx.CommitAsync(ct);
            }
            catch
            {
                await tx.RollbackAsync(ct);
                throw;
            }

            _cache[key] = value;

            // Raise after commit so subscribers see persisted state.
            Changed?.Invoke(this, new SettingsChangedEventArgs(key, value));

            return Result.Ok();
        }
        finally
        {
            gate.Release();
        }
    }
}


