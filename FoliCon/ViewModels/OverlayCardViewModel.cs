using FoliCon.Models.Data;

namespace FoliCon.ViewModels;

/// <summary>
/// ViewModel for a single overlay card in the Overlay Store grid.
/// Displays preview image, metadata, and install/update/remove actions.
/// </summary>
[Localizable(false)]
public class OverlayCardViewModel(OverlayCatalogEntry entry) : BindableBase
{
    public OverlayCatalogEntry CatalogEntry { get; } = entry;

    public string Id { get; } = entry.Id;
    public string DisplayName { get; } = entry.DisplayName;
    public string Author { get; } = entry.Author;
    public string Description { get; } = entry.Description;
    public string OverlayVersion { get; } = entry.OverlayVersion;
    public string[] Tags { get; } = entry.Tags;
    private string PreviewUrl { get; } = entry.PreviewUrl;
    public long SizeBytes { get; } = entry.SizeBytes;

    public string SizeDisplay => SizeBytes switch
    {
        < 1024 => $"{SizeBytes} B",
        < 1024 * 1024 => $"{SizeBytes / 1024.0:F1} KB",
        _ => $"{SizeBytes / (1024.0 * 1024.0):F1} MB"
    };

    public string VersionDisplay => $"v{OverlayVersion}";

    public BitmapSource? PreviewImage
    {
        get;
        set => SetProperty(ref field, value);
    }

    public bool IsInstalled
    {
        get;
        set => SetProperty(ref field, value);
    }

    public bool IsUpdateAvailable
    {
        get;
        set => SetProperty(ref field, value);
    }

    public bool IsLoading
    {
        get;
        set => SetProperty(ref field, value);
    }

    /// <summary>
    /// Downloads and sets the preview image from the URL.
    /// Call after construction; runs async to avoid blocking.
    /// </summary>
    public async Task LoadPreviewAsync(CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(PreviewUrl) || PreviewImage != null)
            return;

        try
        {
            IsLoading = true;
            var bytes = await Services.HttpC.GetByteArrayAsync(PreviewUrl, ct);
            using var stream = new MemoryStream(bytes);
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.StreamSource = stream;
            bitmap.EndInit();
            bitmap.Freeze();
            PreviewImage = bitmap;
        }
        catch (Exception ex)
        {
            // Preview load failure is non-fatal; card shows without image
            System.Diagnostics.Debug.WriteLine($"Failed to load preview for '{Id}': {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }
}
