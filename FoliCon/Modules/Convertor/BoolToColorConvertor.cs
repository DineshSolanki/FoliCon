using NLog;
using Brush = System.Windows.Media.Brush;
using Brushes = System.Windows.Media.Brushes;
using Logger = NLog.Logger;

namespace FoliCon.Modules.Convertor;

public class BoolToColorConverter : IValueConverter
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        Logger.Debug("Converting {Value} to color", value);
        if (value is bool b)
        {
            Logger.Debug("Value is bool");
            return b ? new SolidColorBrush(Colors.Green) : new SolidColorBrush(Colors.Red);
        }
        return Brushes.Transparent;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}