using FitCore.Domain.Enums;

namespace FitCore.Domain.Entities;

public class LicenceRecord
{
    public int Id { get; set; }
    public string RawLicenceJson { get; set; } = string.Empty;
    public string Signature { get; set; } = string.Empty;
    public DateTime VerifiedAtUtc { get; set; }
    public LicenceStatus Status { get; set; }
}
