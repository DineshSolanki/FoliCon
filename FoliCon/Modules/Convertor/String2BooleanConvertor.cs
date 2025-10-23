namespace FoliCon.Modules.Convertor;

public class String2BooleanConvertor : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return parameter?.Equals(value);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value != null && (bool)value ? parameter : DependencyProperty.UnsetValue;
    }
}
