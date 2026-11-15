using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FitCore.Application.Common;

namespace FitCore.Application.Progress;

// ===== DTOs =====

public sealed record BodyMetricRow(
    int Id,
    DateTime RecordedAtUtc,
    decimal? WeightKg,
    decimal? BodyFatPercent,
    string? Notes);

public sealed record WorkoutRow(
    int Id,
    DateTime RecordedAtUtc,
    string ExerciseName,
    int Sets,
    int Reps,
    decimal WeightKg,
    string? Notes);

public sealed record MemberProgressSummary(
    int MemberId,
    string MemberName,
    decimal? LatestWeightKg,
    decimal? WeightChange30d,
    int WorkoutCount30d,
    decimal TotalVolumeLifted,
    string? TopLift,
    string? BiggestGain);

public sealed record WorkoutInput(
    int MemberId,
    string ExerciseName,
    int Sets,
    int Reps,
    decimal WeightKg,
    string? Notes);

public sealed record BodyMetricInput(
    int MemberId,
    decimal? WeightKg,
    decimal? BodyFatPercent,
    string? Notes);

// ===== Interface =====

public interface IProgressService
{
    Task<Result<IReadOnlyList<MemberProgressSummary>>> ListMembersWithProgressAsync(CancellationToken ct = default);
    Task<Result<IReadOnlyList<WorkoutRow>>> ListWorkoutsAsync(int memberId, int limit, CancellationToken ct = default);
    Task<Result<IReadOnlyList<BodyMetricRow>>> ListBodyMetricsAsync(int memberId, int limit, CancellationToken ct = default);
    Task<Result<MemberProgressSummary>> GetSummaryAsync(int memberId, CancellationToken ct = default);
    Task<Result> LogWorkoutAsync(WorkoutInput input, CancellationToken ct = default);
    Task<Result> LogBodyMetricAsync(BodyMetricInput input, CancellationToken ct = default);
}
