namespace FoliCon.Modules.Convertor;
public class TextToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string text && !string.IsNullOrWhiteSpace(text) && text != "Video not available!")
        {
            return Visibility.Visible;
        }

        return parameter as string == "Hidden" ? Visibility.Hidden : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
}