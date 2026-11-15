using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace FitCore.Desktop.Views.Converters;

/// <summary>
/// Parameter "archive": Visible when Status == "Active" or "Inactive".
/// Parameter "restore": Visible when Status == "Archived".
/// </summary>
public sealed class ArchiveActionVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var status = value as string ?? "";
        var param = parameter as string ?? "";
        var isArchived = status.Equals("Archived", StringComparison.OrdinalIgnoreCase);

        if (param == "archive") return isArchived ? Visibility.Collapsed : Visibility.Visible;
        if (param == "restore") return isArchived ? Visibility.Visible : Visibility.Collapsed;
        return Visibility.Collapsed;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
