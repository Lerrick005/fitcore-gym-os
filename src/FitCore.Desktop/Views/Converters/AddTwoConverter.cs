using System;
using System.Globalization;
using System.Windows.Data;

namespace FitCore.Desktop.Views.Converters;

public sealed class AddTwoConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is double d) return d + 2;
        if (value is int i) return (double)(i + 2);
        return 10.0;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
