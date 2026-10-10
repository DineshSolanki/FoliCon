namespace FoliCon.Modules.Convertor;

public class InvertedTextToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value switch
        {
            null => Visibility.Visible,
            string text => text is "" or "Video not available!" ? Visibility.Visible : Visibility.Hidden,
            _ => Visibility.Hidden
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
}
