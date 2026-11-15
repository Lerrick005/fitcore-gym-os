namespace FitCore.Application.Members;

/// <summary>Compact member view for list/search results (PRS §12.1, §12.5).</summary>
public sealed record MemberListItem(
    int Id,
    string MemberNumber,
    string FullName,
    string Phone,
    string? Email,
    string Status,
    DateTime? ArchivedAtUtc);
