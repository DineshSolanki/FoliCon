using Brushes = System.Windows.Media.Brushes;

namespace FoliCon.Modules.Convertor;

public class BoolToColorConverter : IValueConverter
{
    private static readonly SolidColorBrush GreenBrush = MakeBrush(Colors.Green);
    private static readonly SolidColorBrush RedBrush = MakeBrush(Colors.Red);

    private static SolidColorBrush MakeBrush(System.Windows.Media.Color color)
    {
        var brush = new SolidColorBrush(color);
        brush.Freeze();
        return brush;
    }

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not bool b)
        {
            return Brushes.Transparent;
        }

        return b ? GreenBrush : RedBrush;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
}
