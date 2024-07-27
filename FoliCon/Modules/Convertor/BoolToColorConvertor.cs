using Brushes = System.Windows.Media.Brushes;

namespace FoliCon.Modules.Convertor;

public class BoolToColorConverter : IValueConverter
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        Logger.Debug("Converting {Value} to color", value);
        if (value is not bool b)
        {
            return Brushes.Transparent;
        }

        Logger.Debug("Value is bool");
        return b ? new SolidColorBrush(Colors.Green) : new SolidColorBrush(Colors.Red);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}