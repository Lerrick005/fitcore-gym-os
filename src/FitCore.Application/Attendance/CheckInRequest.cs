namespace FitCore.Application.Attendance;

/// <summary>Request to check a member in.</summary>
public sealed record CheckInRequest(int MemberId, string? Notes);

/// <summary>Result of a check-in attempt.</summary>
public sealed record CheckInResult(
    bool IsValid,
    string ResultCode,
    string Message,
    DateTime CheckInUtc);
