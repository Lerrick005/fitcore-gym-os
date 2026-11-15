namespace FitCore.Domain.Entities;

public class Installation
{
    public Guid Id { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public int SchemaVersion { get; set; }
    public string AppVersionAtCreation { get; set; } = string.Empty;
    public bool SetupCompleted { get; set; }
    public int SetupStep { get; set; }
}
