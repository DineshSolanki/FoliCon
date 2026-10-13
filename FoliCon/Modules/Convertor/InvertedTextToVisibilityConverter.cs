namespace FoliCon.Modules.Convertor;

public class InvertedTextToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not string s || string.IsNullOrWhiteSpace(s) || s == "Video not available!")
        {
            return Visibility.Visible;
        }

        return parameter as string == "Hidden" ? Visibility.Hidden : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
}
