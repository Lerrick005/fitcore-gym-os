namespace FitCore.Domain.Entities;

public class SettingsEntry
{
    public int Id { get; set; }
    public string GroupKey { get; set; } = string.Empty;
    public int SchemaVersion { get; set; }
    public string JsonValue { get; set; } = string.Empty;
    public DateTime UpdatedAtUtc { get; set; }
    public int? UpdatedBy { get; set; }
}
