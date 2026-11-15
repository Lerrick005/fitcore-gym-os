using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FitCore.Application.Abstractions;
using FitCore.Application.Common;
using FitCore.Application.Progress;
using FitCore.Domain.Entities;
using FitCore.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FitCore.Infrastructure.Progress;

public sealed class ProgressService : IProgressService
{
    private readonly FitCoreDbContext _db;
    private readonly IClock _clock;

    public ProgressService(FitCoreDbContext db, IClock clock)
    {
        _db = db;
        _clock = clock;
    }

    public async Task<Result<IReadOnlyList<MemberProgressSummary>>> ListMembersWithProgressAsync(CancellationToken ct = default)
    {
        var now = _clock.UtcNow;
        var cutoff30d = now.AddDays(-30);

        var members = await _db.Members.AsQueryable()
            .Where(m => m.ArchivedAtUtc == null)
            .Select(m => new { m.Id, m.FullName })
            .ToListAsync(ct);

        var summaries = new List<MemberProgressSummary>();
        foreach (var m in members)
        {
            var s = await BuildSummaryAsync(m.Id, m.FullName, now, cutoff30d, ct);
            summaries.Add(s);
        }

        // Sort: members with activity first (workout count desc), then alpha
        return Result<IReadOnlyList<MemberProgressSummary>>.Ok(
            summaries.OrderByDescending(s => s.WorkoutCount30d).ThenBy(s => s.MemberName).ToList());
    }

    public async Task<Result<IReadOnlyList<WorkoutRow>>> ListWorkoutsAsync(int memberId, int limit, CancellationToken ct = default)
    {
        var rows = await _db.WorkoutEntries.AsQueryable()
            .Where(w => w.MemberId == memberId)
            .OrderByDescending(w => w.RecordedAtUtc)
            .Take(limit)
            .Select(w => new WorkoutRow(w.Id, w.RecordedAtUtc, w.ExerciseName, w.Sets, w.Reps, w.WeightKg, w.Notes))
            .ToListAsync(ct);
        return Result<IReadOnlyList<WorkoutRow>>.Ok(rows);
    }

    public async Task<Result<IReadOnlyList<BodyMetricRow>>> ListBodyMetricsAsync(int memberId, int limit, CancellationToken ct = default)
    {
        var rows = await _db.ProgressEntries.AsQueryable()
            .Where(p => p.MemberId == memberId && (p.WeightKg.HasValue || p.BodyFatPercent.HasValue))
            .OrderByDescending(p => p.RecordedAtUtc)
            .Take(limit)
            .Select(p => new BodyMetricRow(p.Id, p.RecordedAtUtc, p.WeightKg, p.BodyFatPercent, p.Notes))
            .ToListAsync(ct);
        return Result<IReadOnlyList<BodyMetricRow>>.Ok(rows);
    }

    public async Task<Result<MemberProgressSummary>> GetSummaryAsync(int memberId, CancellationToken ct = default)
    {
        var member = await _db.Members.AsQueryable().FirstOrDefaultAsync(m => m.Id == memberId, ct);
        if (member is null) return Result<MemberProgressSummary>.Fail("member.not_found", "Member not found.");

        var now = _clock.UtcNow;
        var summary = await BuildSummaryAsync(memberId, member.FullName, now, now.AddDays(-30), ct);
        return Result<MemberProgressSummary>.Ok(summary);
    }

    public async Task<Result> LogWorkoutAsync(WorkoutInput input, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(input.ExerciseName))
            return Result.Fail("validation.exercise", "Exercise name is required.");
        if (input.Sets <= 0 || input.Reps <= 0)
            return Result.Fail("validation.sets_reps", "Sets and reps must be positive.");
        if (input.WeightKg < 0)
            return Result.Fail("validation.weight", "Weight cannot be negative.");

        var member = await _db.Members.AsQueryable().FirstOrDefaultAsync(m => m.Id == input.MemberId, ct);
        if (member is null) return Result.Fail("member.not_found", "Member not found.");

        _db.WorkoutEntries.Add(new WorkoutEntry
        {
            MemberId = input.MemberId,
            RecordedAtUtc = _clock.UtcNow,
            ExerciseName = input.ExerciseName.Trim(),
            Sets = input.Sets,
            Reps = input.Reps,
            WeightKg = input.WeightKg,
            Notes = input.Notes,
            CreatedAtUtc = _clock.UtcNow,
            UpdatedAtUtc = _clock.UtcNow
        });
        await _db.SaveChangesAsync(ct);
        return Result.Ok();
    }

    public async Task<Result> LogBodyMetricAsync(BodyMetricInput input, CancellationToken ct = default)
    {
        if (!input.WeightKg.HasValue && !input.BodyFatPercent.HasValue)
            return Result.Fail("validation.empty", "Enter at least one metric (weight or body fat).");

        var member = await _db.Members.AsQueryable().FirstOrDefaultAsync(m => m.Id == input.MemberId, ct);
        if (member is null) return Result.Fail("member.not_found", "Member not found.");

        _db.ProgressEntries.Add(new ProgressEntry
        {
            MemberId = input.MemberId,
            RecordedAtUtc = _clock.UtcNow,
            WeightKg = input.WeightKg,
            BodyFatPercent = input.BodyFatPercent,
            Notes = input.Notes,
            CreatedAtUtc = _clock.UtcNow,
            UpdatedAtUtc = _clock.UtcNow
        });
        await _db.SaveChangesAsync(ct);
        return Result.Ok();
    }

    private async Task<MemberProgressSummary> BuildSummaryAsync(int memberId, string memberName, DateTime now, DateTime cutoff30d, CancellationToken ct)
    {
        // Latest body weight
        var latestBody = await _db.ProgressEntries.AsQueryable()
            .Where(p => p.MemberId == memberId && p.WeightKg.HasValue)
            .OrderByDescending(p => p.RecordedAtUtc)
            .Select(p => new { p.WeightKg, p.RecordedAtUtc })
            .FirstOrDefaultAsync(ct);

        // Weight 30 days ago
        var oldBody = await _db.ProgressEntries.AsQueryable()
            .Where(p => p.MemberId == memberId && p.WeightKg.HasValue && p.RecordedAtUtc <= cutoff30d)
            .OrderByDescending(p => p.RecordedAtUtc)
            .Select(p => p.WeightKg)
            .FirstOrDefaultAsync(ct);

        decimal? weightChange30d = null;
        if (latestBody?.WeightKg is not null && oldBody is not null)
            weightChange30d = latestBody.WeightKg - oldBody;

        // Workouts last 30 days
        var workouts30d = await _db.WorkoutEntries.AsQueryable()
            .Where(w => w.MemberId == memberId && w.RecordedAtUtc >= cutoff30d)
            .OrderBy(w => w.RecordedAtUtc)
            .Select(w => new { w.ExerciseName, w.WeightKg, w.RecordedAtUtc, w.Sets, w.Reps })
            .ToListAsync(ct);

        var workoutCount = workouts30d.Count;
        decimal totalVolume = workouts30d.Sum(w => w.Sets * w.Reps * w.WeightKg);

        // Top lift
        var topLiftEntry = workouts30d.OrderByDescending(w => w.WeightKg).FirstOrDefault();
        string? topLift = topLiftEntry is null ? null 
            : $"{topLiftEntry.ExerciseName}: {topLiftEntry.WeightKg:0.#}kg × {topLiftEntry.Reps}";

        // Biggest gain
        string? biggestGain = null;
        if (workouts30d.Count > 1)
        {
            var byExercise = workouts30d
                .GroupBy(w => w.ExerciseName)
                .Select(g => new
                {
                    Exercise = g.Key,
                    First = g.First().WeightKg,
                    Last = g.Last().WeightKg,
                    Delta = g.Last().WeightKg - g.First().WeightKg
                })
                .Where(x => x.Delta > 0)
                .OrderByDescending(x => x.Delta)
                .FirstOrDefault();

            if (byExercise is not null)
                biggestGain = $"{byExercise.Exercise}: +{byExercise.Delta:0.#}kg in 30 days";
        }

        return new MemberProgressSummary(
            memberId,
            memberName,
            latestBody?.WeightKg,
            weightChange30d,
            workoutCount,
            totalVolume,
            topLift,
            biggestGain);
    }
}

