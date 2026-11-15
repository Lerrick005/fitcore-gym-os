namespace FitCore.Domain.Entities;

public class UserSession
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public DateTime StartedAtUtc { get; set; }
    public DateTime? EndedAtUtc { get; set; }
}
