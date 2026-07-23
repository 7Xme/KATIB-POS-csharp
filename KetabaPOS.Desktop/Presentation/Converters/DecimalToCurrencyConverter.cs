using System.Globalization;
using System.Windows.Data;

namespace KetabaPOS.Desktop.Presentation.Converters;

public class DecimalToCurrencyConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is decimal d) return d.ToString("N2");
        return "0.00";
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (decimal.TryParse(value?.ToString(), out var result)) return result;
        return 0m;
    }
}
