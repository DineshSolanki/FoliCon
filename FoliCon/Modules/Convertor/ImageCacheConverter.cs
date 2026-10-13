using System.Collections.Concurrent;
using System.IO;

namespace FoliCon.Modules.Convertor;

/// <summary>
/// Converts an image path, URL, or local icon to a BitmapImage or BitmapFrame with caching to improve performance and thread safety.
/// </summary>
[Localizable(false)]
public class ImageCacheConverter : IValueConverter
{
    private static readonly ConcurrentDictionary<string, ImageSource> Cache = new();

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        string path = null;
        string folder = null;

        if (value is ListItem item)
        {
            path = item.Poster;
            folder = item.Folder;
        }
        else if (value is string s)
        {
            path = s;
        }

        var cacheKey = !string.IsNullOrWhiteSpace(path) ? path : folder;
        if (string.IsNullOrWhiteSpace(cacheKey))
        {
            return null;
        }

        if (Cache.TryGetValue(cacheKey, out var cachedImage))
        {
            return cachedImage;
        }

        try
        {
            // Case 1: Remote HTTP/HTTPS URL (do NOT call Freeze() while downloading)
            if (!string.IsNullOrWhiteSpace(path) &&
                (path.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                 path.StartsWith("https://", StringComparison.OrdinalIgnoreCase)))
            {
                if (Uri.TryCreate(path, UriKind.Absolute, out var webUri))
                {
                    var image = new BitmapImage();
                    image.BeginInit();
                    image.UriSource = webUri;
                    image.CacheOption = BitmapCacheOption.OnDemand;
                    image.EndInit();
                    Cache.TryAdd(cacheKey, image);
                    return image;
                }
            }

            // Case 2: Resolve local image or icon file
            var resolvedFile = ResolveLocalImagePath(path, folder);

            if (resolvedFile != null && File.Exists(resolvedFile))
            {
                var uri = new Uri(resolvedFile, UriKind.Absolute);

                // If it is an .ico file, use IconBitmapDecoder to extract the highest-resolution frame (up to 256x256)
                if (resolvedFile.EndsWith(".ico", StringComparison.OrdinalIgnoreCase))
                {
                    var decoder = new IconBitmapDecoder(uri, BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
                    var bestFrame = decoder.Frames.OrderByDescending(f => f.PixelWidth).FirstOrDefault();
                    if (bestFrame != null)
                    {
                        if (bestFrame.CanFreeze)
                        {
                            bestFrame.Freeze();
                        }
                        Cache.TryAdd(cacheKey, bestFrame);
                        return bestFrame;
                    }
                }
                else
                {
                    var image = new BitmapImage();
                    image.BeginInit();
                    image.CacheOption = BitmapCacheOption.OnLoad;
                    image.UriSource = uri;
                    image.EndInit();
                    if (image.CanFreeze)
                    {
                        image.Freeze();
                    }
                    Cache.TryAdd(cacheKey, image);
                    return image;
                }
            }

            return null;
        }
        catch
        {
            return null;
        }
    }

    private static string ResolveLocalImagePath(string path, string folder)
    {
        // 1. Direct path exists
        if (!string.IsNullOrWhiteSpace(path) && File.Exists(path))
        {
            return path;
        }

        // 2. Path was a PNG but replaced with .ico
        if (!string.IsNullOrWhiteSpace(path) && path.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
        {
            var ico = Path.ChangeExtension(path, ".ico");
            if (File.Exists(ico))
            {
                return ico;
            }
        }

        // 3. Check within folder if provided
        var targetFolder = !string.IsNullOrWhiteSpace(folder) ? folder : (Directory.Exists(path) ? path : null);
        if (!string.IsNullOrWhiteSpace(targetFolder) && Directory.Exists(targetFolder))
        {
            // Check folicon.png (active processing)
            var foliconPng = Path.Combine(targetFolder, "folicon.png");
            if (File.Exists(foliconPng))
            {
                return foliconPng;
            }

            // Check folicon.ico (completed icon)
            var foliconIco = Path.Combine(targetFolder, "folicon.ico");
            if (File.Exists(foliconIco))
            {
                return foliconIco;
            }

            // Fallback to other common poster/folder images
            var posterJpg = Path.Combine(targetFolder, "poster.jpg");
            if (File.Exists(posterJpg))
            {
                return posterJpg;
            }

            var folderJpg = Path.Combine(targetFolder, "folder.jpg");
            if (File.Exists(folderJpg))
            {
                return folderJpg;
            }
        }

        return null;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotImplementedException("Not implemented.");
}
