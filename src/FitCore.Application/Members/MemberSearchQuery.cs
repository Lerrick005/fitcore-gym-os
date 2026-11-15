namespace FitCore.Application.Members;

/// <summary>Search filters for the Members page (PRS §12.1).</summary>
public sealed class MemberSearchQuery
{
    /// <summary>Matches name, phone or member number (case-insensitive, partial).</summary>
    public string? Text { get; set; }

    /// <summary>Active / Inactive / Archived / All (null = Active + Inactive).</summary>
    public string? Status { get; set; }

    public int Take { get; set; } = 100;
    public int Skip { get; set; } = 0;
}
