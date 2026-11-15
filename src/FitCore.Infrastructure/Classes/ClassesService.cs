using FitCore.Application.Abstractions;
using FitCore.Application.Classes;
using FitCore.Application.Common;
using FitCore.Domain.Entities;
using FitCore.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FitCore.Infrastructure.Classes;

public sealed class ClassesService : IClassesService
{
    private readonly FitCoreDbContext _db;
    private readonly ICurrentUser _user;
    private readonly IClock _clock;
    private readonly IAuditLogger _audit;
    private readonly IModuleAccessGuard _modules;

    public ClassesService(FitCoreDbContext db, ICurrentUser user, IClock clock, IAuditLogger audit, IModuleAccessGuard modules)
    {
        _db = db; _user = user; _clock = clock; _audit = audit; _modules = modules;
    }

    private async Task<Result?> EnsureModuleAsync(CancellationToken ct)
    {
        var r = await _modules.RequireAsync("programmes", ct);
        return r.IsSuccess ? null : r;
    }

    public async Task<Result<IReadOnlyList<ClassRow>>> ListClassesAsync(bool includeInactive, CancellationToken ct = default)
    {
        var m = await EnsureModuleAsync(ct); if (m is not null) return Result<IReadOnlyList<ClassRow>>.Fail(m.ErrorCode!, m.ErrorMessage!);
        if (!_user.HasPermission("programmes.manage")) return Result<IReadOnlyList<ClassRow>>.Fail("permission.denied", "No permission.");

        var q = _db.Programmes.AsQueryable();
        if (!includeInactive) q = q.Where(p => p.IsActive);

        var rows = await q.OrderBy(p => p.Name).ToListAsync(ct);
        var result = new List<ClassRow>();
        foreach (var c in rows)
        {
            var booked = await _db.ProgrammeAssignments.AsQueryable().CountAsync(a => a.ProgrammeId == c.Id && a.EndedAtUtc == null, ct);
            result.Add(new ClassRow(c.Id, c.Name, c.Description, c.Version, c.IsActive, booked, c.StartTime, c.DurationMinutes));
        }
        return Result<IReadOnlyList<ClassRow>>.Ok(result);
    }

    public async Task<Result<ClassRow>> CreateClassAsync(ClassInput input, CancellationToken ct = default)
    {
        var m = await EnsureModuleAsync(ct); if (m is not null) return Result<ClassRow>.Fail(m.ErrorCode!, m.ErrorMessage!);
        if (!_user.HasPermission("programmes.manage")) return Result<ClassRow>.Fail("permission.denied", "No permission.");
        if (string.IsNullOrWhiteSpace(input.Name)) return Result<ClassRow>.Fail("validation.name", "Name required.");

        var p = new Programme
        {
            Name = input.Name.Trim(), Description = input.Description,
            Version = 1, IsActive = true, StartTime = input.StartTime, DurationMinutes = input.DurationMinutes,
            CreatedAtUtc = _clock.UtcNow, UpdatedAtUtc = _clock.UtcNow, CreatedBy = _user.UserId
        };
        _db.Programmes.Add(p);
        await _db.SaveChangesAsync(ct);
        await _audit.LogAsync("class.create", "Programme", p.Id.ToString(), null, p.Name, null, ct);
        return Result<ClassRow>.Ok(new ClassRow(p.Id, p.Name, p.Description, p.Version, p.IsActive, 0, p.StartTime, p.DurationMinutes));
    }

    public async Task<Result<ClassRow>> UpdateClassAsync(int id, ClassInput input, CancellationToken ct = default)
    {
        var m = await EnsureModuleAsync(ct); if (m is not null) return Result<ClassRow>.Fail(m.ErrorCode!, m.ErrorMessage!);
        if (!_user.HasPermission("programmes.manage")) return Result<ClassRow>.Fail("permission.denied", "No permission.");

        var p = await _db.Programmes.AsQueryable().FirstOrDefaultAsync(x => x.Id == id, ct);
        if (p is null) return Result<ClassRow>.Fail("class.not_found", "Class not found.");

        p.Name = input.Name.Trim(); p.Description = input.Description; p.StartTime = input.StartTime; p.DurationMinutes = input.DurationMinutes; p.UpdatedAtUtc = _clock.UtcNow;
        await _db.SaveChangesAsync(ct);
        var booked = await _db.ProgrammeAssignments.AsQueryable().CountAsync(a => a.ProgrammeId == id && a.EndedAtUtc == null, ct);
        return Result<ClassRow>.Ok(new ClassRow(p.Id, p.Name, p.Description, p.Version, p.IsActive, booked, p.StartTime, p.DurationMinutes));
    }

    public async Task<Result> DeactivateClassAsync(int id, CancellationToken ct = default)
    {
        var m = await EnsureModuleAsync(ct); if (m is not null) return m;
        if (!_user.HasPermission("programmes.manage")) return Result.Fail("permission.denied", "No permission.");

        var p = await _db.Programmes.AsQueryable().FirstOrDefaultAsync(x => x.Id == id, ct);
        if (p is null) return Result.Fail("class.not_found", "Class not found.");
        p.IsActive = false;
        p.UpdatedAtUtc = _clock.UtcNow;
        await _db.SaveChangesAsync(ct);
        return Result.Ok();
    }

    public async Task<Result<IReadOnlyList<ClassBookingRow>>> ListBookingsAsync(int? programmeId, CancellationToken ct = default)
    {
        var m = await EnsureModuleAsync(ct); if (m is not null) return Result<IReadOnlyList<ClassBookingRow>>.Fail(m.ErrorCode!, m.ErrorMessage!);
        if (!_user.HasPermission("programmes.manage")) return Result<IReadOnlyList<ClassBookingRow>>.Fail("permission.denied", "No permission.");

        var q = _db.ProgrammeAssignments.AsQueryable();
        if (programmeId.HasValue) q = q.Where(a => a.ProgrammeId == programmeId.Value);

        var rows = await (from a in q
                          join pr in _db.Programmes.AsQueryable() on a.ProgrammeId equals pr.Id
                          join mem in _db.Members.AsQueryable() on a.MemberId equals mem.Id
                          orderby a.AssignedAtUtc descending
                          select new ClassBookingRow(a.Id, a.ProgrammeId, pr.Name, a.MemberId, mem.FullName, a.AssignedAtUtc, a.EndedAtUtc))
                          .Take(500).ToListAsync(ct);
        return Result<IReadOnlyList<ClassBookingRow>>.Ok(rows);
    }

    public async Task<Result> BookAsync(int programmeId, int memberId, CancellationToken ct = default)
    {
        var m = await EnsureModuleAsync(ct); if (m is not null) return m;
        if (!_user.HasPermission("programmes.manage")) return Result.Fail("permission.denied", "No permission.");

        // Prevent double booking
        var existing = await _db.ProgrammeAssignments.AsQueryable()
            .AnyAsync(a => a.ProgrammeId == programmeId && a.MemberId == memberId && a.EndedAtUtc == null, ct);
        if (existing) return Result.Fail("booking.duplicate", "Member already booked in this class.");

        _db.ProgrammeAssignments.Add(new ProgrammeAssignment
        {
            ProgrammeId = programmeId, MemberId = memberId,
            AssignedAtUtc = _clock.UtcNow,
            CreatedAtUtc = _clock.UtcNow, UpdatedAtUtc = _clock.UtcNow, CreatedBy = _user.UserId
        });
        await _db.SaveChangesAsync(ct);
        return Result.Ok();
    }

    public async Task<Result> CancelBookingAsync(int bookingId, CancellationToken ct = default)
    {
        var m = await EnsureModuleAsync(ct); if (m is not null) return m;
        if (!_user.HasPermission("programmes.manage")) return Result.Fail("permission.denied", "No permission.");

        var b = await _db.ProgrammeAssignments.AsQueryable().FirstOrDefaultAsync(a => a.Id == bookingId, ct);
        if (b is null) return Result.Fail("booking.not_found", "Booking not found.");
        b.EndedAtUtc = _clock.UtcNow;
        b.UpdatedAtUtc = _clock.UtcNow;
        await _db.SaveChangesAsync(ct);
        return Result.Ok();
    }
    public async Task<Result<IReadOnlyList<ClassAttendeeRow>>> ListAttendeesAsync(int programmeId, CancellationToken ct = default)
    {
        var m = await EnsureModuleAsync(ct); if (m is not null) return Result<IReadOnlyList<ClassAttendeeRow>>.Fail(m.ErrorCode!, m.ErrorMessage!);
        if (!_user.HasPermission("programmes.manage")) return Result<IReadOnlyList<ClassAttendeeRow>>.Fail("permission.denied", "No permission.");

        var rows = await (from a in _db.ProgrammeAssignments.AsQueryable()
                          join mem in _db.Members.AsQueryable() on a.MemberId equals mem.Id
                          where a.ProgrammeId == programmeId
                          orderby a.AssignedAtUtc
                          select new ClassAttendeeRow(a.Id, a.MemberId, mem.FullName, mem.Phone, a.Status.ToString(), a.AssignedAtUtc))
                          .ToListAsync(ct);
        return Result<IReadOnlyList<ClassAttendeeRow>>.Ok(rows);
    }

    public async Task<Result> UpdateBookingStatusAsync(int bookingId, string status, CancellationToken ct = default)
    {
        var m = await EnsureModuleAsync(ct); if (m is not null) return m;
        if (!_user.HasPermission("programmes.manage")) return Result.Fail("permission.denied", "No permission.");

        var b = await _db.ProgrammeAssignments.AsQueryable().FirstOrDefaultAsync(a => a.Id == bookingId, ct);
        if (b is null) return Result.Fail("booking.not_found", "Booking not found.");

        if (!Enum.TryParse<FitCore.Domain.Enums.BookingStatus>(status, true, out var parsed))
            return Result.Fail("booking.invalid_status", "Invalid status.");

        b.Status = parsed;
        if (parsed == FitCore.Domain.Enums.BookingStatus.Cancelled) b.EndedAtUtc = _clock.UtcNow;
        b.UpdatedAtUtc = _clock.UtcNow;
        await _db.SaveChangesAsync(ct);
        return Result.Ok();
    }

}

