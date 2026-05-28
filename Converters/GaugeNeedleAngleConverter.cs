using System.Globalization;
using System.Windows.Data;

namespace QuickTools.Converters;

public sealed class GaugeNeedleAngleConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var percent = value switch
        {
            int intValue => intValue,
            double doubleValue => doubleValue,
            float floatValue => floatValue,
            _ => 0d
        };

        percent = Math.Clamp(percent, 0d, 100d);
        return -110d + percent * 2.2d;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
