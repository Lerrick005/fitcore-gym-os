namespace FitCore.Application.Abstractions;

/// <summary>Audit trail writer (PRS §4.3, §13.1).</summary>
public interface IAuditLogger
{
    Task LogAsync(
        string action,
        string entityType,
        string? entityId,
        string? oldValue,
        string? newValue,
        string? reason,
        CancellationToken ct = default);
}
