using System;
using System.Globalization;
using System.Windows.Data;

namespace FitCore.Desktop.Views.Converters;

public sealed class BooleanToBooleanInverseConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is not true;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is not true;
}
