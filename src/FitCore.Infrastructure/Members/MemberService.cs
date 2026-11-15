using FitCore.Application.Abstractions;
using FitCore.Application.Members;
using FitCore.Application.Common;
using FitCore.Domain.Entities;
using FitCore.Domain.Enums;
using FitCore.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FitCore.Infrastructure.Members;

public sealed class MemberService : IMemberService
{
    private const string CounterName = "member";
    private const string DefaultPrefix = "MEM-";

    private readonly FitCoreDbContext _db;
    private readonly ICurrentUser _user;
    private readonly IClock _clock;
    private readonly IAuditLogger _audit;

    public MemberService(FitCoreDbContext db, ICurrentUser user, IClock clock, IAuditLogger audit)
    {
        _db = db;
        _user = user;
        _clock = clock;
        _audit = audit;
    }

    public async Task<Result<Member>> CreateAsync(MemberInput input, CancellationToken ct = default)
    {
        if (!_user.HasPermission("members.create"))
            return Result<Member>.Fail("permission.denied", "You do not have permission to create members.");

        var validation = ValidateInput(input);
        if (validation is not null) return Result<Member>.Fail("validation.failed", validation);

        await using var tx = await _db.Database.BeginTransactionAsync(ct);
        try
        {
            var now = _clock.UtcNow;
            var memberNumber = await GenerateMemberNumberAsync(ct);

            var member = new Member
            {
                MemberNumber = memberNumber,
                FullName = input.FullName.Trim(),
                Phone = input.Phone.Trim(),
                Email = string.IsNullOrWhiteSpace(input.Email) ? null : input.Email.Trim(),
                DateOfBirth = input.DateOfBirth,
                Gender = input.Gender,
                EmergencyContact = input.EmergencyContact,
                Notes = input.Notes,
                Status = MemberStatus.Active,
                CreatedAtUtc = now,
                UpdatedAtUtc = now,
                CreatedBy = _user.UserId
            };

            _db.Members.Add(member);
            await _db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);

            await _audit.LogAsync("member.create", "Member", member.Id.ToString(),
                null, $"number={memberNumber} name={member.FullName}", null, ct);

            return Result<Member>.Ok(member);
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }
    }

    public async Task<Result<Member>> UpdateAsync(int memberId, MemberInput input, CancellationToken ct = default)
    {
        if (!_user.HasPermission("members.edit"))
            return Result<Member>.Fail("permission.denied", "You do not have permission to edit members.");

        var validation = ValidateInput(input);
        if (validation is not null) return Result<Member>.Fail("validation.failed", validation);

        var member = await _db.Members.FirstOrDefaultAsync(m => m.Id == memberId, ct);
        if (member is null) return Result<Member>.Fail("member.not_found", "Member not found.");
        if (member.ArchivedAtUtc is not null)
            return Result<Member>.Fail("member.archived", "Cannot edit an archived member.");

        var now = _clock.UtcNow;
        member.FullName = input.FullName.Trim();
        member.Phone = input.Phone.Trim();
        member.Email = string.IsNullOrWhiteSpace(input.Email) ? null : input.Email.Trim();
        member.DateOfBirth = input.DateOfBirth;
        member.Gender = input.Gender;
        member.EmergencyContact = input.EmergencyContact;
        member.Notes = input.Notes;
        member.UpdatedAtUtc = now;

        await _db.SaveChangesAsync(ct);
        await _audit.LogAsync("member.update", "Member", member.Id.ToString(), null, null, null, ct);

        return Result<Member>.Ok(member);
    }

    public async Task<Result> ArchiveAsync(int memberId, string? reason, CancellationToken ct = default)
    {
        if (!_user.HasPermission("members.archive"))
            return Result.Fail("permission.denied", "You do not have permission to archive members.");

        var member = await _db.Members.FirstOrDefaultAsync(m => m.Id == memberId, ct);
        if (member is null) return Result.Fail("member.not_found", "Member not found.");
        if (member.ArchivedAtUtc is not null) return Result.Ok(); // idempotent

        var now = _clock.UtcNow;
        member.ArchivedAtUtc = now;
        member.Status = MemberStatus.Archived;
        member.UpdatedAtUtc = now;
        await _db.SaveChangesAsync(ct);

        await _audit.LogAsync("member.archive", "Member", member.Id.ToString(), null, null, reason, ct);
        return Result.Ok();
    }

    public async Task<Result<IReadOnlyList<MemberListItem>>> SearchAsync(MemberSearchQuery query, CancellationToken ct = default)
    {
        if (!_user.HasPermission("members.view"))
            return Result<IReadOnlyList<MemberListItem>>.Fail("permission.denied", "You do not have permission to view members.");

        var q = _db.Members.AsQueryable();

        // Status filter (default: non-archived)
        switch (query.Status?.ToLowerInvariant())
        {
            case "archived":
                q = q.Where(m => m.ArchivedAtUtc != null);
                break;
            case "all":
                break; // no filter
            case "active":
                q = q.Where(m => m.ArchivedAtUtc == null && m.Status == MemberStatus.Active);
                break;
            case "inactive":
                q = q.Where(m => m.ArchivedAtUtc == null && m.Status == MemberStatus.Inactive);
                break;
            default:
                q = q.Where(m => m.ArchivedAtUtc == null);
                break;
        }

        // Text filter
        if (!string.IsNullOrWhiteSpace(query.Text))
        {
            var search = query.Text.Trim();
            q = q.Where(m =>
                EF.Functions.Like(m.FullName, $"%{search}%")
                || EF.Functions.Like(m.Phone, $"%{search}%")
                || EF.Functions.Like(m.MemberNumber, $"%{search}%"));
        }

        var results = await q
            .OrderBy(m => m.FullName)
            .Skip(query.Skip)
            .Take(Math.Max(1, Math.Min(query.Take, 500)))
            .Select(m => new MemberListItem(
                m.Id,
                m.MemberNumber,
                m.FullName,
                m.Phone,
                m.Email,
                m.Status.ToString(),
                m.ArchivedAtUtc))
            .ToListAsync(ct);

        return Result<IReadOnlyList<MemberListItem>>.Ok(results);
    }

    public async Task<Result> RestoreAsync(int memberId, CancellationToken ct = default)
    {
        if (!_user.HasPermission("members.archive"))
            return Result.Fail("permission.denied", "You do not have permission to restore members.");

        var member = await _db.Members.FirstOrDefaultAsync(m => m.Id == memberId, ct);
        if (member is null) return Result.Fail("member.not_found", "Member not found.");
        if (member.ArchivedAtUtc is null) return Result.Ok();

        var now = _clock.UtcNow;
        member.ArchivedAtUtc = null;
        member.Status = MemberStatus.Active;
        member.UpdatedAtUtc = now;
        await _db.SaveChangesAsync(ct);

        await _audit.LogAsync("member.restore", "Member", member.Id.ToString(), null, null, null, ct);
        return Result.Ok();
    }

    public async Task<Result<Member>> GetAsync(int memberId, CancellationToken ct = default)
    {
        if (!_user.HasPermission("members.view"))
            return Result<Member>.Fail("permission.denied", "You do not have permission to view members.");

        var member = await _db.Members.FirstOrDefaultAsync(m => m.Id == memberId, ct);
        return member is null
            ? Result<Member>.Fail("member.not_found", "Member not found.")
            : Result<Member>.Ok(member);
    }

    private static string? ValidateInput(MemberInput input)
    {
        if (string.IsNullOrWhiteSpace(input.FullName))
            return "Full name is required.";
        if (input.FullName.Length > 200)
            return "Full name must be 200 characters or fewer.";
        if (string.IsNullOrWhiteSpace(input.Phone))
            return "Phone is required.";
        if (input.Phone.Length > 30)
            return "Phone must be 30 characters or fewer.";
        if (!string.IsNullOrWhiteSpace(input.Email) && !input.Email.Contains('@'))
            return "Email does not appear to be valid.";
        return null;
    }

    private async Task<string> GenerateMemberNumberAsync(CancellationToken ct)
    {
        var counter = await _db.SequenceCounters.FirstOrDefaultAsync(c => c.Name == CounterName, ct);
        if (counter is null)
        {
            counter = new SequenceCounter
            {
                Name = CounterName,
                Prefix = DefaultPrefix,
                NextValue = 1
            };
            _db.SequenceCounters.Add(counter);
            await _db.SaveChangesAsync(ct);
        }

        var number = $"{counter.Prefix}{counter.NextValue:D6}";
        counter.NextValue += 1;
        await _db.SaveChangesAsync(ct);
        return number;
    }
}


