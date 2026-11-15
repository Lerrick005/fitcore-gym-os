namespace FitCore.Domain.Entities;

public class SequenceCounter
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Prefix { get; set; } = string.Empty;
    public long NextValue { get; set; }
}
