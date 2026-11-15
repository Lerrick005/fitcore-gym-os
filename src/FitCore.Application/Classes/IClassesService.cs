using FitCore.Application.Common;

namespace FitCore.Application.Classes;

public interface IClassesService
{
    Task<Result<IReadOnlyList<ClassRow>>> ListClassesAsync(bool includeInactive, CancellationToken ct = default);
    Task<Result<ClassRow>> CreateClassAsync(ClassInput input, CancellationToken ct = default);
    Task<Result<ClassRow>> UpdateClassAsync(int id, ClassInput input, CancellationToken ct = default);
    Task<Result> DeactivateClassAsync(int id, CancellationToken ct = default);

    Task<Result<IReadOnlyList<ClassBookingRow>>> ListBookingsAsync(int? programmeId, CancellationToken ct = default);
    Task<Result> BookAsync(int programmeId, int memberId, CancellationToken ct = default);
    Task<Result> CancelBookingAsync(int bookingId, CancellationToken ct = default);
    Task<Result<IReadOnlyList<ClassAttendeeRow>>> ListAttendeesAsync(int programmeId, CancellationToken ct = default);
    Task<Result> UpdateBookingStatusAsync(int bookingId, string status, CancellationToken ct = default);
}
