using System.Globalization;
using System.Windows.Data;
namespace KetabaPOS.Desktop.Presentation.Converters;
public class ChartBarConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is decimal dec)
        {
            var max = parameter != null && decimal.TryParse(parameter.ToString(), out var m) && m > 0 ? m : 1000m;
            var ratio = Math.Min((double)(dec / max), 1.0);
            return Math.Max(ratio * 160, 4);
        }
        return 4;
    }
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotImplementedException();
}
