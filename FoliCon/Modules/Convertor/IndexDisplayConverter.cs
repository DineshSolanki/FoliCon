namespace FoliCon.Modules.Convertor;

/// <summary>
/// Converts a 0-based index to a 1-based display string (e.g. 0 -> "#1").
/// </summary>
public class IndexDisplayConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) => value is int index and >= 0 ? $"#{index + 1}" : string.Empty;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotImplementedException();
}
