using FitCore.Application.Abstractions;
using FitCore.Application.Common;
using FitCore.Application.Retention;
using FitCore.Domain.Entities;
using FitCore.Domain.Enums;
using FitCore.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FitCore.Infrastructure.Retention;

public sealed class RetentionService : IRetentionService
{
    private readonly FitCoreDbContext _db;
    private readonly ICurrentUser _user;
    private readonly IClock _clock;
    private readonly IAuditLogger _audit;
    private readonly IModuleAccessGuard _modules;

    public RetentionService(FitCoreDbContext db, ICurrentUser user, IClock clock, IAuditLogger audit, IModuleAccessGuard modules)
    {
        _db = db; _user = user; _clock = clock; _audit = audit; _modules = modules;
    }

    private async Task<Result?> EnsureModuleAsync(CancellationToken ct)
    {
        var r = await _modules.RequireAsync("retention", ct);
        return r.IsSuccess ? null : r;
    }

    public async Task<Result<IReadOnlyList<AtRiskMember>>> GetAtRiskAsync(int inactivityDays, CancellationToken ct = default)
    {
        var m = await EnsureModuleAsync(ct); if (m is not null) return Result<IReadOnlyList<AtRiskMember>>.Fail(m.ErrorCode!, m.ErrorMessage!);
        if (!_user.HasPermission("followups.manage")) return Result<IReadOnlyList<AtRiskMember>>.Fail("permission.denied", "No permission.");

        var cutoff = _clock.UtcNow.AddDays(-inactivityDays);

        // 1. Get active members
        var activeMembers = await _db.Members.AsQueryable()
            .Where(m => m.Status == MemberStatus.Active && m.ArchivedAtUtc == null)
            .Select(m => new { m.Id, m.MemberNumber, m.FullName, m.Phone })   
            .ToListAsync(ct);

        // 2. Get their last check-in
        var lastCheckIns = await _db.AttendanceRecords.AsQueryable()
            .GroupBy(a => a.MemberId)
            .Select(g => new { MemberId = g.Key, LastCheckIn = g.Max(x => x.CheckInUtc) })
            .ToListAsync(ct);       
        var checkInMap = lastCheckIns.ToDictionary(c => c.MemberId);

        // 3. Find existing open follow-ups so we dont duplicate
        var existingFollowUps = await _db.FollowUps.AsQueryable()
            .Where(f => f.Status == FollowUpStatus.Open)
            .Select(f => f.MemberId)
            .ToListAsync(ct);
        var openFollowUpSet = new HashSet<int>(existingFollowUps);

        var results = new List<AtRiskMember>();
        var newFollowUpsCreated = false;

        foreach (var mem in activeMembers)
        {
            checkInMap.TryGetValue(mem.Id, out var last);
            var lastCheckIn = last?.LastCheckIn ?? DateTime.MinValue;
            
            if (lastCheckIn < cutoff)
            {
                var days = lastCheckIn == DateTime.MinValue ? 0 : (int)(_clock.UtcNow - lastCheckIn).TotalDays;      
                var hasFollowUp = openFollowUpSet.Contains(mem.Id);
                results.Add(new AtRiskMember(mem.Id, mem.MemberNumber, mem.FullName, mem.Phone, lastCheckIn, days, hasFollowUp));

                // AUTOMATION: If they are at risk and have NO open follow-up, create one.
                if (!hasFollowUp)
                {
                    _db.FollowUps.Add(new FollowUp
                    {
                        MemberId = mem.Id,
                        DueAtUtc = _clock.UtcNow.AddDays(1), // Action required tomorrow
                        Notes = days == 0 ? "Auto-generated: Member has never checked in." : $"Auto-generated: No visit for {days} days.",
                        Status = FollowUpStatus.Open,
                        CreatedAtUtc = _clock.UtcNow,
                        UpdatedAtUtc = _clock.UtcNow,
                        CreatedBy = _user.UserId
                    });
                    newFollowUpsCreated = true;
                }
            }
        }

        if (newFollowUpsCreated)
        {
            await _db.SaveChangesAsync(ct);
        }

        return Result<IReadOnlyList<AtRiskMember>>.Ok(results.OrderByDescending(r => r.DaysSinceLastVisit).ToList());   
    }

    public async Task<Result<IReadOnlyList<FollowUpItem>>> ListFollowUpsAsync(CancellationToken ct = default)
    {
        var m = await EnsureModuleAsync(ct); if (m is not null) return Result<IReadOnlyList<FollowUpItem>>.Fail(m.ErrorCode!, m.ErrorMessage!);
        if (!_user.HasPermission("followups.manage")) return Result<IReadOnlyList<FollowUpItem>>.Fail("permission.denied", "No permission.");

        var rows = await (from f in _db.FollowUps.AsQueryable()
                          join mm in _db.Members.AsQueryable() on f.MemberId equals mm.Id
                          where f.Status == FollowUpStatus.Open
                          orderby f.DueAtUtc
                          select new FollowUpItem(f.Id, f.MemberId, mm.FullName, f.DueAtUtc, f.Notes, f.Status.ToString(), f.Outcome))
                          .ToListAsync(ct);
        return Result<IReadOnlyList<FollowUpItem>>.Ok(rows);
    }

    public async Task<Result<FollowUpItem>> CreateFollowUpAsync(FollowUpInput input, CancellationToken ct = default)
    {
        var m = await EnsureModuleAsync(ct); if (m is not null) return Result<FollowUpItem>.Fail(m.ErrorCode!, m.ErrorMessage!);
        if (!_user.HasPermission("followups.manage")) return Result<FollowUpItem>.Fail("permission.denied", "No permission.");

        var member = await _db.Members.AsQueryable().FirstOrDefaultAsync(x => x.Id == input.MemberId, ct);
        if (member is null) return Result<FollowUpItem>.Fail("member.not_found", "Member not found.");

        var f = new FollowUp
        {
            MemberId = input.MemberId,
            DueAtUtc = input.DueAtUtc,
            Notes = input.Notes,
            Status = FollowUpStatus.Open,
            CreatedAtUtc = _clock.UtcNow, UpdatedAtUtc = _clock.UtcNow, CreatedBy = _user.UserId
        };
        _db.FollowUps.Add(f);
        await _db.SaveChangesAsync(ct);
        return Result<FollowUpItem>.Ok(new FollowUpItem(f.Id, f.MemberId, member.FullName, f.DueAtUtc, f.Notes, "Open", null));
    }

    public async Task<Result> CompleteFollowUpAsync(int id, string outcome, CancellationToken ct = default)
    {
        var m = await EnsureModuleAsync(ct); if (m is not null) return m;
        if (!_user.HasPermission("followups.manage")) return Result.Fail("permission.denied", "No permission.");

        var f = await _db.FollowUps.AsQueryable().FirstOrDefaultAsync(x => x.Id == id, ct);
        if (f is null) return Result.Fail("followup.not_found", "Follow-up not found.");

        f.Status = FollowUpStatus.Done;
        f.Outcome = outcome;
        f.CompletedAtUtc = _clock.UtcNow;
        f.UpdatedAtUtc = _clock.UtcNow;
        await _db.SaveChangesAsync(ct);
        await _audit.LogAsync("followup.complete", "FollowUp", id.ToString(), null, outcome, null, ct);
        return Result.Ok();
    }

    public async Task<Result<IReadOnlyList<NewMemberRow>>> ListNewMembersAsync(int windowDays, CancellationToken ct = default)
    {
        var m = await EnsureModuleAsync(ct); if (m is not null) return Result<IReadOnlyList<NewMemberRow>>.Fail(m.ErrorCode!, m.ErrorMessage!);
        if (!_user.HasPermission("followups.manage")) return Result<IReadOnlyList<NewMemberRow>>.Fail("permission.denied", "No permission.");

        var now = _clock.UtcNow;
        var cutoff = now.AddDays(-windowDays);

        var firstStarts = await _db.Memberships.AsQueryable()
            .GroupBy(x => x.MemberId)
            .Select(g => new { MemberId = g.Key, FirstStart = g.Min(x => x.StartDate) })
            .Where(x => x.FirstStart >= cutoff)
            .ToListAsync(ct);

        var memberIds = firstStarts.Select(x => x.MemberId).ToList();
        if (memberIds.Count == 0) return Result<IReadOnlyList<NewMemberRow>>.Ok(new List<NewMemberRow>());

        var members = await _db.Members.AsQueryable()
            .Where(mm => memberIds.Contains(mm.Id))
            .Select(mm => new { mm.Id, mm.MemberNumber, mm.FullName, mm.Phone })
            .ToListAsync(ct);
        var memberMap = members.ToDictionary(x => x.Id);

        var checkIns = await _db.AttendanceRecords.AsQueryable()
            .Where(a => memberIds.Contains(a.MemberId))
            .Select(a => new { a.MemberId, a.CheckInUtc })
            .ToListAsync(ct);
        var checkInsByMember = checkIns.GroupBy(a => a.MemberId).ToDictionary(g => g.Key, g => g.ToList());

        var rows = new List<NewMemberRow>();
        foreach (var fs in firstStarts)
        {
            if (!memberMap.TryGetValue(fs.MemberId, out var mem)) continue;
            var joined = fs.FirstStart;
            var daysSince = (int)(now - joined).TotalDays;

            checkInsByMember.TryGetValue(fs.MemberId, out var list);
            list ??= new();
            var first30Cutoff = joined.AddDays(30);
            var visitsFirst30 = list.Count(a => a.CheckInUtc >= joined && a.CheckInUtc < first30Cutoff);
            var lastVisit = list.Count > 0 ? list.Max(x => x.CheckInUtc) : (DateTime?)null;
            var daysSinceLastVisit = lastVisit.HasValue ? (int)(now - lastVisit.Value).TotalDays : 9999;

            string flag;
            if (visitsFirst30 == 0) flag = "No visit yet";
            else if (visitsFirst30 < 4) flag = "<4 visits in first 30d";
            else if (daysSinceLastVisit >= 14) flag = "Inactive after day 30";
            else flag = "On track";

            rows.Add(new NewMemberRow(mem.Id, mem.MemberNumber, mem.FullName, mem.Phone, joined, daysSince, visitsFirst30, flag));
        }

        return Result<IReadOnlyList<NewMemberRow>>.Ok(rows.OrderBy(r => r.Flag == "On track").ThenByDescending(r => r.DaysSinceJoined).ToList());
    }
}


