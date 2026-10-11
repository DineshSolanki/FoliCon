using System.Globalization;
using System.IO;
using System.Windows.Data;

namespace FoliCon.Modules.Convertor;

/// <summary>
/// Converts a fileName and directory path into an ImageSource via ImageCacheConverter.
/// </summary>
public class CustomIconPathConverter : IMultiValueConverter
{
    private static readonly ImageCacheConverter CacheConverter = new();

    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values != null && values.Length >= 2 &&
            values[0] is string fileName && !string.IsNullOrWhiteSpace(fileName) &&
            values[1] is string directory && !string.IsNullOrWhiteSpace(directory))
        {
            var fullPath = Path.IsPathRooted(fileName) ? fileName : Path.Combine(directory, fileName);
            return CacheConverter.Convert(fullPath, targetType, parameter, culture);
        }

        return null;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) =>
        throw new NotImplementedException();
}
