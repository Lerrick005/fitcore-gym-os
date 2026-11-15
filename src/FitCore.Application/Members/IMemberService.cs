using FitCore.Application.Common;
using FitCore.Domain.Entities;

namespace FitCore.Application.Members;

/// <summary>
/// Member lifecycle (PRS §12.1). Members with history are archived, never hard-deleted.
/// </summary>
public interface IMemberService
{
    /// <summary>Create a new member; generates the member number from the sequence counter.</summary>
    Task<Result<Member>> CreateAsync(MemberInput input, CancellationToken ct = default);

    /// <summary>Update an existing member's editable fields (does not touch member number or status).</summary>
    Task<Result<Member>> UpdateAsync(int memberId, MemberInput input, CancellationToken ct = default);

    /// <summary>Archive a member. Never hard-deletes.</summary>
    Task<Result> ArchiveAsync(int memberId, string? reason, CancellationToken ct = default);

    /// <summary>Restore an archived member. Reverses ArchiveAsync.</summary>
    Task<Result> RestoreAsync(int memberId, CancellationToken ct = default);

    /// <summary>Search members by text + status. Returns a compact list for the grid.</summary>
    Task<Result<IReadOnlyList<MemberListItem>>> SearchAsync(MemberSearchQuery query, CancellationToken ct = default);

    /// <summary>Get a single member entity (full).</summary>
    Task<Result<Member>> GetAsync(int memberId, CancellationToken ct = default);
}

