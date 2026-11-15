using FitCore.Application.Common;
using FitCore.Domain.Entities;

namespace FitCore.Application.Training;

public interface ITrainerService
{
    Task<Result<IReadOnlyList<TrainerListItem>>> ListAsync(bool includeInactive, CancellationToken ct = default);
    Task<Result<Trainer>> GetAsync(int id, CancellationToken ct = default);
    Task<Result<Trainer>> CreateAsync(TrainerInput input, CancellationToken ct = default);
    Task<Result<Trainer>> UpdateAsync(int id, TrainerInput input, CancellationToken ct = default);
    Task<Result> SetActiveAsync(int id, bool isActive, CancellationToken ct = default);
}
