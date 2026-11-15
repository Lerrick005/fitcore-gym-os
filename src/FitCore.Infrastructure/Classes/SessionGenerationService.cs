using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FitCore.Application.Abstractions;
using FitCore.Domain.Entities;
using FitCore.Domain.Enums;
using FitCore.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FitCore.Infrastructure.Classes;

public interface ISessionGenerationService
{
    Task EnsureSessionsAsync(int daysAhead, CancellationToken ct = default);
}

public sealed class SessionGenerationService : ISessionGenerationService
{
    private readonly FitCoreDbContext _db;
    private readonly IClock _clock;

    public SessionGenerationService(FitCoreDbContext db, IClock clock)
    {
        _db = db;
        _clock = clock;
    }

    public async Task EnsureSessionsAsync(int daysAhead, CancellationToken ct = default)
    {
        var today = _clock.UtcNow.Date;
        var horizon = today.AddDays(daysAhead);

        var programmes = await _db.Programmes.AsQueryable()
            .Where(p => p.IsActive && p.StartTime.HasValue)
            .ToListAsync(ct);

        foreach (var p in programmes)
        {
            var current = today;
            while (current <= horizon)
            {
                if (current.DayOfWeek == p.StartTime!.Value.DayOfWeek)
                {
                    var sessionDate = current.Add(p.StartTime.Value.TimeOfDay);
                    
                    // Skip past-dated sessions (only generate for today onward)
                    if (sessionDate >= _clock.UtcNow)
                    {
                        var exists = await _db.ClassSessions.AsQueryable()
                            .AnyAsync(s => s.ProgrammeId == p.Id && s.SessionDate == sessionDate, ct);
                        if (!exists)
                        {
                            _db.ClassSessions.Add(new ClassSession
                            {
                                ProgrammeId = p.Id,
                                SessionDate = sessionDate,
                                Status = SessionStatus.Scheduled,
                                CreatedAtUtc = _clock.UtcNow,
                                UpdatedAtUtc = _clock.UtcNow
                            });
                        }
                    }
                }
                current = current.AddDays(1);
            }
        }

        await _db.SaveChangesAsync(ct);
    }
}
