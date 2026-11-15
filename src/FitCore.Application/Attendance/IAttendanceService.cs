using FitCore.Application.Common;

namespace FitCore.Application.Attendance;

public interface IAttendanceService
{
    Task<Result<CheckInResult>> CheckInAsync(CheckInRequest request, CancellationToken ct = default);
    Task<Result<IReadOnlyList<AttendanceListItem>>> ListForMemberAsync(int memberId, CancellationToken ct = default);
    Task<Result<IReadOnlyList<AttendanceListItem>>> ListRecentAsync(int take, CancellationToken ct = default);
    Task<Result<int>> TodayCheckInCountAsync(CancellationToken ct = default);

    /// <summary>Check out a member currently checked in. Returns the duration in minutes.</summary>
    Task<Result<int>> CheckOutAsync(int memberId, CancellationToken ct = default);

    /// <summary>Members currently checked in (checked-in, not checked-out, on same business day).</summary>
    Task<Result<IReadOnlyList<AttendanceListItem>>> ListCurrentlyCheckedInAsync(CancellationToken ct = default);
}

