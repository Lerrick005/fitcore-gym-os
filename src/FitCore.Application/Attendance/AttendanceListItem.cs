namespace FitCore.Application.Attendance;

public sealed record AttendanceListItem(
    int Id,
    int MemberId,
    DateTime CheckInUtc,
    DateTime? CheckOutUtc,
    string Method,
    string Result);
