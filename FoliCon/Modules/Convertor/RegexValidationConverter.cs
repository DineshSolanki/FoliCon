#nullable enable
namespace FoliCon.Modules.Convertor;

/// <summary>
/// Validates test input against a regex pattern. Returns true for match, false for mismatch,
/// and null when input is empty or null so default styles apply.
/// </summary>
public class RegexValidationConverter : IMultiValueConverter
{
    public object? Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length < 2)
        {
            return null;
        }

        var input = values[0] as string;
        var pattern = values[1] as string;

        if (string.IsNullOrWhiteSpace(input) || string.IsNullOrWhiteSpace(pattern))
        {
            return null;
        }

        try
        {
            var regex = new Regex(pattern, RegexOptions.None, TimeSpan.FromSeconds(1));
            return regex.IsMatch(input);
        }
        catch (ArgumentException) // Handle invalid regex patterns
        {
            return false;
        }
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) =>
        throw new NotImplementedException();
}
