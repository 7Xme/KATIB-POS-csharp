using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace KetabaPOS.Desktop.Presentation.Converters;

public class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var boolValue = value is true;
        var invert = parameter?.ToString() == "Invert";
        return (boolValue ^ invert) ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is Visibility.Visible;
    }
}
