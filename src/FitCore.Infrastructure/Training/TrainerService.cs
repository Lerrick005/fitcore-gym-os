using FitCore.Application.Abstractions;
using FitCore.Application.Common;
using FitCore.Application.Training;
using FitCore.Domain.Entities;
using FitCore.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FitCore.Infrastructure.Training;

public sealed class TrainerService : ITrainerService
{
    private readonly FitCoreDbContext _db;
    private readonly ICurrentUser _user;
    private readonly IClock _clock;
    private readonly IAuditLogger _audit;
    private readonly IModuleAccessGuard _modules;

    public TrainerService(FitCoreDbContext db, ICurrentUser user, IClock clock, IAuditLogger audit, IModuleAccessGuard modules)
    {
        _db = db;
        _user = user;
        _clock = clock;
        _audit = audit;
        _modules = modules;
    }

    private async Task<Result?> EnsureModuleAsync(CancellationToken ct)
    {
        var r = await _modules.RequireAsync("trainers", ct);
        if (!r.IsSuccess) return r;
        return null;
    }

    public async Task<Result<IReadOnlyList<TrainerListItem>>> ListAsync(bool includeInactive, CancellationToken ct = default)
    {
        var m = await EnsureModuleAsync(ct);
        if (m is not null) return Result<IReadOnlyList<TrainerListItem>>.Fail(m.ErrorCode!, m.ErrorMessage!);

        if (!_user.HasPermission("trainers.manage"))
            return Result<IReadOnlyList<TrainerListItem>>.Fail("permission.denied", "You do not have permission to view trainers.");

        var q = _db.Trainers.AsQueryable();
        if (!includeInactive) q = q.Where(t => t.IsActive);

        var rows = await q.OrderBy(t => t.FullName).ToListAsync(ct);

        var list = new List<TrainerListItem>();
        foreach (var t in rows)
        {
            var assigned = await _db.TrainerAssignments.AsQueryable()
                .CountAsync(a => a.TrainerId == t.Id && a.EndedAtUtc == null, ct);
            list.Add(new TrainerListItem(t.Id, t.FullName, t.Phone, t.Email, t.Specialities, t.CommissionRate, t.IsActive, assigned));
        }

        return Result<IReadOnlyList<TrainerListItem>>.Ok(list);
    }

    public async Task<Result<Trainer>> GetAsync(int id, CancellationToken ct = default)
    {
        var m = await EnsureModuleAsync(ct);
        if (m is not null) return Result<Trainer>.Fail(m.ErrorCode!, m.ErrorMessage!);

        var t = await _db.Trainers.FirstOrDefaultAsync(x => x.Id == id, ct);
        return t is null ? Result<Trainer>.Fail("trainer.not_found", "Trainer not found.") : Result<Trainer>.Ok(t);
    }

    public async Task<Result<Trainer>> CreateAsync(TrainerInput input, CancellationToken ct = default)
    {
        var m = await EnsureModuleAsync(ct);
        if (m is not null) return Result<Trainer>.Fail(m.ErrorCode!, m.ErrorMessage!);

        if (!_user.HasPermission("trainers.manage"))
            return Result<Trainer>.Fail("permission.denied", "You do not have permission to manage trainers.");
        if (string.IsNullOrWhiteSpace(input.FullName))
            return Result<Trainer>.Fail("validation.name", "Full name is required.");

        var now = _clock.UtcNow;
        var trainer = new Trainer
        {
            FullName = input.FullName.Trim(),
            Phone = input.Phone,
            Email = input.Email,
            Specialities = input.Specialities,
            CommissionRate = input.CommissionRate,
            IsActive = true,
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
            CreatedBy = _user.UserId
        };
        _db.Trainers.Add(trainer);
        await _db.SaveChangesAsync(ct);
        await _audit.LogAsync("trainer.create", "Trainer", trainer.Id.ToString(), null, trainer.FullName, null, ct);
        return Result<Trainer>.Ok(trainer);
    }

    public async Task<Result<Trainer>> UpdateAsync(int id, TrainerInput input, CancellationToken ct = default)
    {
        var m = await EnsureModuleAsync(ct);
        if (m is not null) return Result<Trainer>.Fail(m.ErrorCode!, m.ErrorMessage!);

        if (!_user.HasPermission("trainers.manage"))
            return Result<Trainer>.Fail("permission.denied", "You do not have permission to manage trainers.");

        var t = await _db.Trainers.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (t is null) return Result<Trainer>.Fail("trainer.not_found", "Trainer not found.");

        t.FullName = input.FullName.Trim();
        t.Phone = input.Phone;
        t.Email = input.Email;
        t.Specialities = input.Specialities;
        t.CommissionRate = input.CommissionRate;
        t.UpdatedAtUtc = _clock.UtcNow;
        await _db.SaveChangesAsync(ct);
        await _audit.LogAsync("trainer.update", "Trainer", t.Id.ToString(), null, t.FullName, null, ct);
        return Result<Trainer>.Ok(t);
    }

    public async Task<Result> SetActiveAsync(int id, bool isActive, CancellationToken ct = default)
    {
        var m = await EnsureModuleAsync(ct);
        if (m is not null) return m;

        if (!_user.HasPermission("trainers.manage"))
            return Result.Fail("permission.denied", "You do not have permission to manage trainers.");

        var t = await _db.Trainers.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (t is null) return Result.Fail("trainer.not_found", "Trainer not found.");

        t.IsActive = isActive;
        t.UpdatedAtUtc = _clock.UtcNow;
        await _db.SaveChangesAsync(ct);
        await _audit.LogAsync(isActive ? "trainer.activate" : "trainer.deactivate", "Trainer", t.Id.ToString(), null, null, null, ct);
        return Result.Ok();
    }
}
