using System.Globalization;
using System.Windows.Data;

namespace QuickTools.Converters;

public sealed class BoolToTextConverter : IValueConverter
{
    public string TrueText { get; set; } = "On";
    public string FalseText { get; set; } = "Off";

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is true ? TrueText : FalseText;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return Binding.DoNothing;
    }
}
