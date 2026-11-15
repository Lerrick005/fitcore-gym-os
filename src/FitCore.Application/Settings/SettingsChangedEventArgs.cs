namespace FitCore.Application.Settings;

public sealed class SettingsChangedEventArgs : EventArgs
{
    public string GroupKey { get; }
    public object NewValue { get; }

    public SettingsChangedEventArgs(string groupKey, object newValue)
    {
        GroupKey = groupKey;
        NewValue = newValue;
    }
}
