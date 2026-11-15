using FitCore.Domain.Enums;

namespace FitCore.Domain.Entities;

public class AttendanceRecord : AuditableEntity
{
    public int MemberId { get; set; }
    public DateTime CheckInUtc { get; set; }
    public DateTime? CheckOutUtc { get; set; }
    public CheckInMethod Method { get; set; }
    public AttendanceResult Result { get; set; }
    public int RecordedBy { get; set; }
    public string? CorrectionReason { get; set; }
}
