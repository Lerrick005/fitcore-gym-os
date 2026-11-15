namespace FitCore.Application.Licensing;

/// <summary>Licence payload signed by vendor (PRS §14).</summary>
public sealed record LicencePayload(
    string LicenceId,
    string CustomerName,
    string InstallationId,
    string AttributionMode,      // "hidden", "visible"
    string AttributionText,
    string EditionLabel,         // "Standard", "Pro", etc.
    IReadOnlyList<string> EntitledModules,
    DateTime IssuedAtUtc,
    DateTime? ExpiresAtUtc);

public enum LicenceState
{
    Missing,
    Valid,
    Expired,
    WrongInstallation,
    Tampered
}

public sealed record LicenceStatus(
    LicenceState State,
    LicencePayload? Payload,
    string Message);
