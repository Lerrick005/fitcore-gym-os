using FitCore.Domain.Enums;

namespace FitCore.Domain.Entities;

public class BackupHistory
{
    public int Id { get; set; }
    public DateTime TimestampUtc { get; set; }
    public BackupType Type { get; set; }
    public string Path { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public BackupResult Result { get; set; }
}
