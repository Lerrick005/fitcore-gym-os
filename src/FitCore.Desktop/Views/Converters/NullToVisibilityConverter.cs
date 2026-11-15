using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace FitCore.Desktop.Views.Converters;

/// <summary>
/// Visible when value is null OR when value is not null, depending on ConverterParameter.
/// Parameter "null"    → visible when the value IS null.
/// Parameter "notnull" → visible when the value is NOT null.
/// No parameter → visible when value is not null (default).
/// </summary>
public sealed class NullToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var param = parameter as string;
        var isNull = value is null;

        return param switch
        {
            "null"    => isNull ? Visibility.Visible : Visibility.Collapsed,
            "notnull" => isNull ? Visibility.Collapsed : Visibility.Visible,
            _         => isNull ? Visibility.Collapsed : Visibility.Visible
        };
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
