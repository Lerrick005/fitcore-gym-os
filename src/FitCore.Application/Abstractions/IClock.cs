namespace FitCore.Application.Abstractions;

/// <summary>Testable clock (PRS §4.3). Always returns UTC.</summary>
public interface IClock
{
    DateTime UtcNow { get; }
}
