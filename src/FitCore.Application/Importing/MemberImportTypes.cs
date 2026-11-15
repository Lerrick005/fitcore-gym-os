namespace FitCore.Application.Importing;

/// <summary>One row parsed from a CSV file (header-based column mapping).</summary>
public sealed record MemberImportRow(
    int RowNumber,
    string FullName,
    string Phone,
    string? Email,
    string? DateOfBirth,
    string? Gender,
    string? Notes);

/// <summary>Result of one row after validation + duplicate check.</summary>
public sealed record MemberImportRowResult(
    int RowNumber,
    string FullName,
    string Phone,
    string Status,      // "Ready", "Duplicate", "Invalid", "Error"
    string? Message);

/// <summary>Summary of a dry-run.</summary>
public sealed record MemberImportPreview(
    int TotalRows,
    int ReadyCount,
    int DuplicateCount,
    int InvalidCount,
    IReadOnlyList<MemberImportRowResult> RowResults);
