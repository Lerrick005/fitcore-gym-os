namespace FitCore.Application.Settings;

/// <summary>Marker for typed settings groups (PRS §6). Each record declares its GroupKey.</summary>
public interface ISettingsGroup
{
    static abstract string GroupKey { get; }
}
