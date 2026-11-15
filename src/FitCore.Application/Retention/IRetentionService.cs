using FitCore.Application.Common;

namespace FitCore.Application.Retention;

public interface IRetentionService
{
    Task<Result<IReadOnlyList<AtRiskMember>>> GetAtRiskAsync(int inactivityDays, CancellationToken ct = default);
    Task<Result<IReadOnlyList<FollowUpItem>>> ListFollowUpsAsync(CancellationToken ct = default);
    Task<Result<FollowUpItem>> CreateFollowUpAsync(FollowUpInput input, CancellationToken ct = default);
    Task<Result> CompleteFollowUpAsync(int id, string outcome, CancellationToken ct = default);
    Task<Result<IReadOnlyList<NewMemberRow>>> ListNewMembersAsync(int windowDays, CancellationToken ct = default);
}
