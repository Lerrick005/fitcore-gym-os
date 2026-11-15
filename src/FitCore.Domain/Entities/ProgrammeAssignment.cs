using FitCore.Domain.Enums;
namespace FitCore.Domain.Entities;

public class ProgrammeAssignment : AuditableEntity
{
    public int ProgrammeId { get; set; }
    public int? SessionId { get; set; }
    public int MemberId { get; set; }
    public DateTime AssignedAtUtc { get; set; }
    public DateTime? EndedAtUtc { get; set; }
    public string? Notes { get; set; }
    public BookingStatus Status { get; set; } = BookingStatus.Booked;
}


