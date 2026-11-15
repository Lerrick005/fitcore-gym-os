namespace FitCore.Domain.Entities;

/// <summary>System-seeded module catalogue and per-installation enable state (PRS §9.2, §13.1).</summary>
public class ModuleState
{
    public int Id { get; set; }
    public string Key { get; set; } = string.Empty;
    public bool IsEnabled { get; set; }
    public DateTime? EnabledAtUtc { get; set; }
    public DateTime? DisabledAtUtc { get; set; }
}
