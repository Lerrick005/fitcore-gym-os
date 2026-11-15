namespace FitCore.Application.Training;

public sealed record TrainerInput(string FullName, string? Phone, string? Email, string? Specialities, decimal? CommissionRate);
public sealed record TrainerListItem(int Id, string FullName, string? Phone, string? Email, string? Specialities, decimal? CommissionRate, bool IsActive, int AssignedMembers);
