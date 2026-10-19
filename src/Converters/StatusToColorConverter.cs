using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace WIFInonSignalingTool.Converters;

public class StatusToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool connected)
            return connected
                ? new SolidColorBrush(Color.FromRgb(76, 175, 80))
                : new SolidColorBrush(Color.FromRgb(244, 67, 54));
        return new SolidColorBrush(Colors.Gray);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

public class PassFailColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string s)
        {
            return s switch
            {
                "PASS" => new SolidColorBrush(Color.FromRgb(76, 175, 80)),
                "FAIL" => new SolidColorBrush(Color.FromRgb(244, 67, 54)),
                "Testing" => new SolidColorBrush(Color.FromRgb(255, 152, 0)),
                _ => new SolidColorBrush(Color.FromRgb(136, 153, 170))
            };
        }
        return new SolidColorBrush(Color.FromRgb(136, 153, 170));
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
