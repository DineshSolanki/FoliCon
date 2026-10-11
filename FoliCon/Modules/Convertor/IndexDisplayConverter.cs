using System.Globalization;
using System.Windows.Data;

namespace FoliCon.Modules.Convertor;

/// <summary>
/// Converts a 0-based index to a 1-based display string (e.g. 0 -> "#1").
/// </summary>
public class IndexDisplayConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is int index && index >= 0)
        {
            return $"#{index + 1}";
        }

        return string.Empty;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotImplementedException();
}
