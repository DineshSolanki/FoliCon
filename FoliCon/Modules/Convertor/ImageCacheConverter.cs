using System.Collections.Concurrent;

namespace FoliCon.Modules.Convertor;

/// <summary>
/// Converts an image path, URL, or local icon to a BitmapImage or BitmapFrame with caching to improve performance and thread safety.
/// </summary>
[Localizable(false)]
public class ImageCacheConverter : IValueConverter
{
    private static readonly ConcurrentDictionary<string, ImageSource> Cache = new();
    private static readonly string[] FolderCandidateNames = ["folicon.png", "folicon.ico", "poster.jpg", "folder.jpg"];

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var (path, folder) = ExtractPathAndFolder(value);

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
            if (IsWebUrl(path))
            {
                var webImage = CreateWebImage(path);
                if (webImage != null)
                {
                    Cache.TryAdd(cacheKey, webImage);
                }
                return webImage;
            }

            var resolvedFile = ResolveLocalImagePath(path, folder);
            if (resolvedFile != null && File.Exists(resolvedFile))
            {
                var localImage = LoadLocalImage(resolvedFile);
                if (localImage != null)
                {
                    Cache.TryAdd(cacheKey, localImage);
                }
                return localImage;
            }

            return null;
        }
        catch
        {
            return null;
        }
    }

    private static (string path, string folder) ExtractPathAndFolder(object value)
    {
        if (value is ListItem item)
        {
            return (item.Poster, item.Folder);
        }

        if (value is string s)
        {
            return (s, null);
        }

        return (null, null);
    }

    private static bool IsWebUrl(string path) =>
        !string.IsNullOrWhiteSpace(path) &&
        (path.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
         path.StartsWith("https://", StringComparison.OrdinalIgnoreCase));

    private static BitmapImage CreateWebImage(string path)
    {
        if (Uri.TryCreate(path, UriKind.Absolute, out var webUri))
        {
            var image = new BitmapImage();
            image.BeginInit();
            image.UriSource = webUri;
            image.CacheOption = BitmapCacheOption.OnDemand;
            image.DownloadFailed += (_, _) =>
            {
                ((ICollection<KeyValuePair<string, ImageSource>>)Cache)
                    .Remove(new KeyValuePair<string, ImageSource>(path, image));
            };
            image.EndInit();
            return image;
        }

        return null;
    }

    private static ImageSource LoadLocalImage(string resolvedFile)
    {
        var uri = new Uri(resolvedFile, UriKind.Absolute);

        if (resolvedFile.EndsWith(".ico", StringComparison.OrdinalIgnoreCase))
        {
            return LoadIcoFrame(uri);
        }

        var image = new BitmapImage();
        image.BeginInit();
        image.CacheOption = BitmapCacheOption.OnLoad;
        image.UriSource = uri;
        image.EndInit();
        if (image.CanFreeze)
        {
            image.Freeze();
        }
        return image;
    }

    private static BitmapFrame LoadIcoFrame(Uri uri)
    {
        var decoder = new IconBitmapDecoder(uri, BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
        var bestFrame = decoder.Frames.OrderByDescending(f => f.PixelWidth).FirstOrDefault();
        if (bestFrame != null && bestFrame.CanFreeze)
        {
            bestFrame.Freeze();
        }
        return bestFrame;
    }

    private static string ResolveLocalImagePath(string path, string folder)
    {
        if (!string.IsNullOrWhiteSpace(path))
        {
            if (File.Exists(path))
            {
                return path;
            }

            if (path.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
            {
                var ico = Path.ChangeExtension(path, ".ico");
                if (File.Exists(ico))
                {
                    return ico;
                }
            }
        }

        var targetFolder = DetermineTargetFolder(path, folder);
        return FindFolderImage(targetFolder);
    }

    private static string DetermineTargetFolder(string path, string folder)
    {
        if (!string.IsNullOrWhiteSpace(folder))
        {
            return folder;
        }

        if (!string.IsNullOrWhiteSpace(path) && Directory.Exists(path))
        {
            return path;
        }

        return null;
    }

    private static string FindFolderImage(string targetFolder)
    {
        if (string.IsNullOrWhiteSpace(targetFolder) || !Directory.Exists(targetFolder))
        {
            return null;
        }

        foreach (var fileName in FolderCandidateNames)
        {
            var candidate = Path.Combine(targetFolder, fileName);
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        return null;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotImplementedException("Not implemented.");
}
