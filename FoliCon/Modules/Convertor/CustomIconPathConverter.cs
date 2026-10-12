using System.Globalization;
using System.IO;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace FoliCon.Modules.Convertor;

/// <summary>
/// Converts a file name and directory path into a small, uncached icon preview.
/// </summary>
public class CustomIconPathConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values != null && values.Length >= 2 &&
            values[0] is string fileName && !string.IsNullOrWhiteSpace(fileName) &&
            values[1] is string directory && !string.IsNullOrWhiteSpace(directory))
        {
            var fullPath = Path.IsPathRooted(fileName) ? fileName : Path.Combine(directory, fileName);
            if (!File.Exists(fullPath))
            {
                return null;
            }

            var image = new BitmapImage();
            image.BeginInit();
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.DecodePixelWidth = 22;
            image.UriSource = new Uri(fullPath, UriKind.Absolute);
            image.EndInit();
            image.Freeze();
            return image;
        }

        return null;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) =>
        throw new NotImplementedException();
}
