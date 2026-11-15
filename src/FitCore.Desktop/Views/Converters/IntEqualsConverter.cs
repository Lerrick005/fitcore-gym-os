using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace FitCore.Desktop.Views.Converters;

public sealed class IntEqualsConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is int i && int.TryParse(parameter as string, out var target))
            return i == target ? Visibility.Visible : Visibility.Collapsed;
        return Visibility.Collapsed;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
