namespace FitCore.Application.Classes;

public sealed record ClassRow(int Id, string Name, string? Description, int Version, bool IsActive, int BookedCount, DateTime? StartTime, int DurationMinutes);
public sealed record ClassInput(string Name, string? Description, DateTime? StartTime, int DurationMinutes);
public sealed record ClassBookingRow(int Id, int ProgrammeId, string ProgrammeName, int MemberId, string MemberName, DateTime AssignedAtUtc, DateTime? EndedAtUtc);


public sealed record ClassAttendeeRow(int BookingId, int MemberId, string MemberName, string Phone, string Status, DateTime BookedAtUtc);
