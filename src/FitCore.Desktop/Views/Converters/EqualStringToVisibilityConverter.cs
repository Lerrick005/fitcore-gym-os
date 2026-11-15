using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace FitCore.Desktop.Views.Converters;

public sealed class EqualStringToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var str = value as string ?? "";
        var param = parameter as string ?? "";
        return string.Equals(str, param, StringComparison.Ordinal) ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
