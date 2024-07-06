namespace FoliCon.Modules.Convertor;

public class RegexValidationConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        var input = values[0] as string;
        var pattern = values[1] as string;

        if (string.IsNullOrWhiteSpace(input) || string.IsNullOrWhiteSpace(pattern))
        {
            return false;
        }

        try
        {
            var regex = new Regex(pattern);
            return regex.IsMatch(input);
        }
        catch (ArgumentException) // Handle invalid regex patterns
        {
            return false;
        }
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}