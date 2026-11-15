namespace FitCore.Application.Members;

/// <summary>Request to create or update a member (PRS §12.1).</summary>
public sealed record MemberInput(
    string FullName,
    string Phone,
    string? Email,
    DateTime? DateOfBirth,
    string? Gender,
    string? EmergencyContact,
    string? Notes);
