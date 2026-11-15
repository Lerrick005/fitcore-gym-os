using FitCore.Application.Abstractions;

namespace FitCore.Infrastructure.Services;

/// <summary>Sole permitted source of wall-clock time (PRS §8.1).</summary>
public sealed class SystemClock : IClock
{
    public DateTime UtcNow => DateTime.UtcNow;
}
