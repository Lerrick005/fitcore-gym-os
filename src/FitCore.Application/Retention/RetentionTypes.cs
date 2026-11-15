namespace FitCore.Application.Retention;

public sealed record AtRiskMember(int MemberId, string MemberNumber, string FullName, string Phone, DateTime LastCheckInUtc, int DaysSinceLastVisit, bool HasOpenFollowUp);
public sealed record FollowUpItem(int Id, int MemberId, string MemberName, DateTime DueAtUtc, string? Notes, string Status, string? Outcome);
public sealed record FollowUpInput(int MemberId, DateTime DueAtUtc, string? Notes);
public sealed record NewMemberRow(int MemberId, string MemberNumber, string FullName, string Phone, DateTime JoinedUtc, int DaysSinceJoined, int VisitsInFirst30Days, string Flag);
