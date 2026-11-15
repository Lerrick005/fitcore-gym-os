namespace FitCore.Application.Reports;

public sealed record RevenueByDay(DateTime Date, long AmountMinor, string CurrencyCode);
public sealed record RevenueByMethod(string Method, long AmountMinor, int Count);
public sealed record ExpiringMember(int MemberId, string MemberNumber, string FullName, string Phone, DateTime EndDate, string PlanName);
public sealed record AttendanceByDay(DateTime Date, int Count);
public sealed record MemberGrowthByDay(DateTime Date, int NewMembers, int TotalCumulative);
public sealed record RevenueByMonth(int Year, int Month, long AmountMinor, string CurrencyCode);
public sealed record MemberGrowthByMonth(int Year, int Month, int NewMembers);

public sealed record ReportsData(
    IReadOnlyList<RevenueByDay> RevenueLast30Days,
    IReadOnlyList<RevenueByMethod> RevenueByMethodLast30Days,
    IReadOnlyList<ExpiringMember> ExpiringInNext30Days,
    IReadOnlyList<AttendanceByDay> AttendanceLast30Days,
    long RevenueLast30DaysTotal,
    int NewMembersLast30Days,
    int ActiveMembers,

    // New analytics
    IReadOnlyList<RevenueByMonth> RevenueLast12Months,
    IReadOnlyList<MemberGrowthByDay> MemberGrowthLast30Days,
    IReadOnlyList<MemberGrowthByMonth> MemberGrowthLast12Months,
    long RevenueYearToDate,
    int MembersAtStartOfYear,
    int MembersAtStartOfMonth,
    long RevenueThisMonth,
    long RevenueLastMonth,
    string CurrencyCode);
