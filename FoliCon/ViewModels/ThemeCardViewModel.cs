#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using FoliCon.Models.Themes;
using FoliCon.Modules.Themes;
using Prism.Mvvm;
using Color = System.Windows.Media.Color;

namespace FoliCon.ViewModels;

/// <summary>
/// ViewModel representing a single theme card in the Unified Store or Theme Picker.
/// Displays live color swatch pills, metadata, and install/preview actions.
/// </summary>
public class ThemeCardViewModel : BindableBase
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    public ThemeCatalogEntry? CatalogEntry { get; }
    public ThemeManifest? InstalledManifest { get; }

    public string Id { get; }
    public string Name { get; }
    public string Description { get; }
    public string Author { get; }
    public string Version { get; }
    public string BaseTheme { get; }
    public List<string> Tags { get; }
    public double Rating { get; }
    public int Downloads { get; }
    public bool IsBuiltIn { get; }

    // Color Swatches for Palette Visualization
    public SolidColorBrush SwatchBackground { get; }
    public SolidColorBrush SwatchCard { get; }
    public SolidColorBrush SwatchPrimary { get; }
    public SolidColorBrush SwatchAccent { get; }
    public SolidColorBrush SwatchText { get; }

    public ThemeCardViewModel(ThemeManifest manifest, bool isInstalled = true, bool isUpdateAvailable = false, string? availableVersion = null)
    {
        InstalledManifest = manifest ?? throw new ArgumentNullException(nameof(manifest));
        Id = manifest.Id;
        Name = manifest.Name;
        Description = manifest.Description;
        Author = manifest.Author;
        Version = manifest.Version;
        BaseTheme = manifest.BaseTheme;
        Tags = manifest.Tags ?? [];
        IsBuiltIn = manifest.IsBuiltIn;
        Rating = 5.0;
        Downloads = 0;

        IsInstalled = isInstalled;
        IsUpdateAvailable = isUpdateAvailable;
        AvailableVersion = availableVersion;

        var (bg, card, primary, accent, text) = ExtractSwatches(manifest.Colors, manifest.IsDark);
        SwatchBackground = bg;
        SwatchCard = card;
        SwatchPrimary = primary;
        SwatchAccent = accent;
        SwatchText = text;

        LoadLocalPreviewImage(manifest);
    }

    public ThemeCardViewModel(ThemeCatalogEntry entry, bool isInstalled, bool isUpdateAvailable, string? installedVersion = null)
    {
        CatalogEntry = entry ?? throw new ArgumentNullException(nameof(entry));
        Id = entry.Id;
        Name = entry.Name;
        Description = entry.Description;
        Author = entry.Author;
        Version = entry.Version;
        BaseTheme = entry.BaseTheme;
        Tags = entry.Tags ?? [];
        IsBuiltIn = ThemeConstants.BuiltInThemeIds.Contains(entry.Id);
        Rating = entry.Rating;
        Downloads = entry.Downloads;

        IsInstalled = isInstalled;
        IsUpdateAvailable = isUpdateAvailable;
        InstalledVersion = installedVersion;
        AvailableVersion = entry.Version;

        var isDark = !string.Equals(entry.BaseTheme, "Light", StringComparison.OrdinalIgnoreCase);
        var (bg, card, primary, accent, text) = ExtractSwatches(entry.Palette, isDark);
        SwatchBackground = bg;
        SwatchCard = card;
        SwatchPrimary = primary;
        SwatchAccent = accent;
        SwatchText = text;
    }

    #region State Properties

    public bool IsInstalled
    {
        get;
        set => SetProperty(ref field, value);
    }

    public bool IsActive
    {
        get;
        set => SetProperty(ref field, value);
    }

    public bool IsUpdateAvailable
    {
        get;
        set => SetProperty(ref field, value);
    }

    public string? InstalledVersion
    {
        get;
        set => SetProperty(ref field, value);
    }

    public string? AvailableVersion
    {
        get;
        set => SetProperty(ref field, value);
    }

    public bool IsLoading
    {
        get;
        set => SetProperty(ref field, value);
    }

    public int ProgressPercentage
    {
        get;
        set => SetProperty(ref field, value);
    }

    public string? OperationMessage
    {
        get;
        set => SetProperty(ref field, value);
    }

    public BitmapSource? PreviewImage
    {
        get;
        set => SetProperty(ref field, value);
    }

    #endregion

    private void LoadLocalPreviewImage(ThemeManifest manifest)
    {
        if (string.IsNullOrEmpty(manifest.PackageDirectory) || string.IsNullOrEmpty(manifest.PreviewImageFile))
        {
            return;
        }

        var fullPath = Path.Combine(manifest.PackageDirectory, manifest.PreviewImageFile);
        if (!File.Exists(fullPath)) return;

        try
        {
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.UriSource = new Uri(fullPath);
            bitmap.EndInit();
            bitmap.Freeze();
            PreviewImage = bitmap;
        }
        catch (Exception ex)
        {
            Logger.Warn(ex, "Failed to load preview image for theme '{Id}'", manifest.Id);
        }
    }

    private static (SolidColorBrush bg, SolidColorBrush card, SolidColorBrush primary, SolidColorBrush accent, SolidColorBrush text)
        ExtractSwatches(ThemeColorPalette? palette, bool isDark)
    {
        var defaultBg = isDark ? Color.FromRgb(0x14, 0x14, 0x14) : Color.FromRgb(0xF8, 0xF9, 0xFA);
        var defaultCard = isDark ? Color.FromRgb(0x1E, 0x1E, 0x1E) : Color.FromRgb(0xFF, 0xFF, 0xFF);
        var defaultPrimary = Color.FromRgb(0x3B, 0x82, 0xF6);
        var defaultAccent = Color.FromRgb(0x60, 0xA5, 0xFA);
        var defaultText = isDark ? Color.FromRgb(0xF8, 0xFA, 0xFC) : Color.FromRgb(0x0F, 0x17, 0x2A);

        if (palette == null)
        {
            return (Freeze(defaultBg), Freeze(defaultCard), Freeze(defaultPrimary), Freeze(defaultAccent), Freeze(defaultText));
        }

        var bg = TryParseColor(palette.Background, defaultBg);
        var card = TryParseColor(palette.CardSurface, defaultCard);
        var primary = TryParseColor(palette.Primary, defaultPrimary);
        var accent = TryParseColor(palette.Accent, defaultAccent);
        var text = TryParseColor(palette.PrimaryText, defaultText);

        return (Freeze(bg), Freeze(card), Freeze(primary), Freeze(accent), Freeze(text));
    }

    private static Color TryParseColor(string? hex, Color fallback)
    {
        if (string.IsNullOrWhiteSpace(hex)) return fallback;
        try
        {
            var clean = hex.Trim().TrimStart('#');
            if (clean.Length == 6)
            {
                return Color.FromRgb(
                    Convert.ToByte(clean[..2], 16),
                    Convert.ToByte(clean.Substring(2, 2), 16),
                    Convert.ToByte(clean.Substring(4, 2), 16));
            }
            if (clean.Length == 8)
            {
                return Color.FromArgb(
                    Convert.ToByte(clean[..2], 16),
                    Convert.ToByte(clean.Substring(2, 2), 16),
                    Convert.ToByte(clean.Substring(4, 2), 16),
                    Convert.ToByte(clean.Substring(6, 2), 16));
            }
        }
        catch
        {
            // fallback on format exception
        }
        return fallback;
    }

    private static SolidColorBrush Freeze(Color color)
    {
        var brush = new SolidColorBrush(color);
        brush.Freeze();
        return brush;
    }
}
