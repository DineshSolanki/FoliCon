namespace FoliCon.Modules.Convertor;

/// <summary>
/// Converts an image path to a BitmapImage with caching to improve performance and thread safety.
/// CREDIT: https://stackoverflow.com/a/37652158/8076598
/// </summary>
public class ImageCacheConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var path = (string)value;
        if (string.IsNullOrEmpty(path))
        {
            return null;
        }
        var uri = new Uri(path, UriKind.Absolute);
        try
        {
            var image = new BitmapImage();
            image.BeginInit();
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.UriSource = uri;
            image.EndInit();
            image.Freeze(); // Improve performance and thread safety
            return image;
        }
        catch
        {
            return null; // Return null or a default image in case of error
        }
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException("Not implemented.");
    }
}