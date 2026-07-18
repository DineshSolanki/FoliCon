using FoliCon.Models.Data;
using Brush = System.Windows.Media.Brush;
using Brushes = System.Windows.Media.Brushes;
using Point = System.Windows.Point;
using FontFamily = System.Windows.Media.FontFamily;

namespace FoliCon.Views;

/// <summary>
/// Generic data-driven poster icon renderer that builds its visual tree
/// from a PosterOverlayDefinition at runtime.
/// </summary>
public partial class DynamicPosterIcon : PosterIconBase
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    private const string ShieldImagePath = "/Resources/rating_mockup/shield.png";

    private static readonly string[] DefaultLayerOrder = ["base", "poster", "front", "rating", "title"];

    public DynamicPosterIcon(PosterOverlayDefinition definition, object dataContext)
        : base(dataContext)
    {
        InitializeComponent();
        DataContext = dataContext;
        Width = definition.DesignWidth;
        Height = definition.DesignHeight;
        BuildVisualTree(definition, dataContext);
    }

    private void BuildVisualTree(PosterOverlayDefinition definition, object dataContext)
    {
        var rootMargin = ParseThickness(definition.RootMargin);
        var rootGrid = new Grid { Margin = rootMargin };

        // Cache created elements by key
        var elements = new Dictionary<string, UIElement>();

        // --- Base Layer ---
        if (definition.BaseLayer != null && !string.IsNullOrEmpty(definition.BaseLayer.ImagePath))
        {
            var baseImage = CreateLayerImage(definition.BaseLayer.ImagePath, definition.BaseLayer.Margin);
            if (baseImage != null)
            {
                baseImage.SetBinding(VisibilityProperty, new Binding("MockupVisibility") { Source = dataContext });
                elements["base"] = baseImage;
            }
        }

        // --- Poster Image (with optional clip and opacity mask) ---
        var posterElement = CreatePosterElement(definition);
        if (posterElement != null)
            elements["poster"] = posterElement;

        // --- Front Layer ---
        if (definition.FrontLayer != null && !string.IsNullOrEmpty(definition.FrontLayer.ImagePath))
        {
            var frontImage = CreateLayerImage(definition.FrontLayer.ImagePath, definition.FrontLayer.Margin);
            if (frontImage != null)
            {
                frontImage.SetBinding(VisibilityProperty, new Binding("MockupVisibility") { Source = dataContext });
                elements["front"] = frontImage;
            }
        }

        // --- Title Text ---
        TextBlock? titleBlock = null;
        var titleInRatingGrid = false;
        var titleGridRow = 0;
        if (definition.Title != null && definition.Title.IsVisible)
        {
            titleBlock = CreateTitleText(definition.Title, dataContext);
            titleInRatingGrid = titleBlock != null &&
                string.Equals(definition.Title.Container, "RatingGrid", StringComparison.OrdinalIgnoreCase);
            titleGridRow = definition.Title.GridRow;
        }

        // --- Rating Grid ---
        elements["rating"] = CreateRatingGrid(
            definition.Rating,
            dataContext,
            titleInRatingGrid ? titleBlock : null,
            titleGridRow);

        if (titleBlock != null && !titleInRatingGrid)
            elements["title"] = titleBlock;

        // --- Add children in the order specified by LayerOrder (matches original XAML z-order) ---
        var layerOrder = definition.LayerOrder ?? DefaultLayerOrder;
        foreach (var key in layerOrder)
        {
            if (elements.TryGetValue(key, out var element))
                rootGrid.Children.Add(element);
        }

        Content = rootGrid;
    }

    private Image? CreateLayerImage(string imagePath, string margin)
    {
        try
        {
            // No Stretch — matches original XAML where base/front images have no Stretch attribute.
            // WPF default is Stretch.Uniform (natural size, aspect ratio preserved).
            var image = new Image
            {
                Source = ResolveImageSource(imagePath),
                Margin = ParseThickness(margin)
            };
            RenderOptions.SetBitmapScalingMode(image, BitmapScalingMode.HighQuality);
            return image;
        }
        catch (Exception ex)
        {
            Logger.Warn(ex, "Failed to create layer image for '{ImagePath}'", imagePath);
            return null;
        }
    }

    private UIElement? CreatePosterElement(PosterOverlayDefinition definition)
    {
        var hasClip = definition.Poster.ClipRadius != "0" &&
                      !string.IsNullOrWhiteSpace(definition.Poster.ClipRadius);
        var hasOpacityMask = !string.IsNullOrEmpty(definition.Poster.OpacityMaskPath);

        // No clip and no opacity mask — plain Image with margin (direct child of root Grid)
        if (!hasClip && !hasOpacityMask)
        {
            var posterImage = new Image
            {
                Source = GetPosterImageSource(),
                Stretch = Stretch.Fill,
                Margin = ParseThickness(definition.Poster.Margin)
            };
            RenderOptions.SetBitmapScalingMode(posterImage, BitmapScalingMode.HighQuality);
            return posterImage;
        }

        // Opacity mask only (no clip) — plain Image with margin and OpacityMask, no Border wrapper.
        // Matches original XAML where Windows11 poster is a direct Image in the Grid.
        if (!hasClip && hasOpacityMask)
        {
            var posterImage = new Image
            {
                Source = GetPosterImageSource(),
                Stretch = Stretch.Fill,
                Margin = ParseThickness(definition.Poster.Margin)
            };
            posterImage.OpacityMask = new ImageBrush(ResolveImageSource(definition.Poster.OpacityMaskPath!))
            {
                Stretch = Stretch.Fill
            };
            RenderOptions.SetBitmapScalingMode(posterImage, BitmapScalingMode.HighQuality);
            return posterImage;
        }

        // Clip (with optional opacity mask) — wrap in Border.
        // The Border gets the margin and clip; the Image inside may have its own margin (PosterInnerMargin).
        var border = new Border
        {
            Background = Brushes.Transparent,
            Margin = ParseThickness(definition.Poster.Margin)
        };

        var posterImageInner = new Image
        {
            Source = GetPosterImageSource(),
            Stretch = Stretch.Fill
        };

        // Some overlays need a small margin on the inner Image (e.g. faelpessoal "0,0,0,-1")
        if (!string.IsNullOrWhiteSpace(definition.Poster.PosterInnerMargin))
            posterImageInner.Margin = ParseThickness(definition.Poster.PosterInnerMargin);

        RenderOptions.SetBitmapScalingMode(posterImageInner, BitmapScalingMode.HighQuality);

        var cornerRadius = ParseCornerRadius(definition.Poster.ClipRadius);
        border.CornerRadius = cornerRadius;

        // Use explicit ClipRect if provided, otherwise calculate from margins.
        if (!string.IsNullOrWhiteSpace(definition.Poster.ClipRect))
        {
            var rectParts = definition.Poster.ClipRect!.Split(',');
            if (rectParts.Length == 4)
            {
                var rx = ParseDouble(rectParts[0]);
                var ry = ParseDouble(rectParts[1]);
                var rw = ParseDouble(rectParts[2]);
                var rh = ParseDouble(rectParts[3]);
                border.Clip = new RectangleGeometry(
                    new Rect(rx, ry, rw, rh),
                    cornerRadius.TopLeft, cornerRadius.TopLeft);
            }
        }
        else
        {
            // Fallback: calculate from design dimensions and margins
            var rootMargin = ParseThickness(definition.RootMargin);
            var posterMargin = ParseThickness(definition.Poster.Margin);
            var clipWidth = definition.DesignWidth - posterMargin.Left - posterMargin.Right;
            var effectiveHeight = definition.DesignHeight
                + Math.Abs(Math.Min(0, rootMargin.Top))
                + Math.Abs(Math.Min(0, rootMargin.Bottom));
            var clipHeight = effectiveHeight - posterMargin.Top - posterMargin.Bottom;

            border.Clip = new RectangleGeometry(
                new Rect(0, 0, clipWidth, clipHeight),
                cornerRadius.TopLeft, cornerRadius.TopLeft);
        }

        if (hasOpacityMask)
        {
            posterImageInner.OpacityMask = new ImageBrush(ResolveImageSource(definition.Poster.OpacityMaskPath!))
            {
                Stretch = Stretch.Fill
            };
        }

        border.Child = posterImageInner;
        return border;
    }

    private Grid CreateRatingGrid(
        RatingConfig rating,
        object dataContext,
        TextBlock? titleBlock = null,
        int titleGridRow = 0)
    {
        var grid = new Grid();
        grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(100) });
        grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(100) });

        if (titleBlock != null)
        {
            Grid.SetRow(titleBlock, Math.Clamp(titleGridRow, 0, grid.RowDefinitions.Count - 1));
            grid.Children.Add(titleBlock);
        }

        var shield = new Image
        {
            Source = ResolveImageSource(ShieldImagePath),
            Margin = ParseThickness(rating.ShieldMargin)
        };
        RenderOptions.SetBitmapScalingMode(shield, BitmapScalingMode.HighQuality);
        Panel.SetZIndex(shield, 2);
        Grid.SetRow(shield, 1);
        Grid.SetRowSpan(shield, 2);
        shield.SetBinding(VisibilityProperty, new Binding("RatingVisibility") { Source = dataContext });
        grid.Children.Add(shield);

        var ratingText = new TextBlock
        {
            // Use font name directly — WPF's built-in fallback chain handles missing fonts,
            // matching the original XAML behavior exactly.
            FontFamily = new FontFamily(rating.FontFamily),
            FontStyle = FontStyles.Italic,
            FontSize = rating.FontSize,
            Foreground = Brushes.Black,
            Width = rating.TextWidth,
            Height = rating.TextHeight,
            Margin = ParseThickness(rating.TextMargin)
        };
        Panel.SetZIndex(ratingText, 3);
        Grid.SetRow(ratingText, 2);
        ratingText.HorizontalAlignment = ParseHorizontalAlignment(rating.TextHorizontalAlignment);
        ratingText.VerticalAlignment = ParseVerticalAlignment(rating.TextVerticalAlignment);
        ratingText.SetBinding(VisibilityProperty, new Binding("RatingVisibility") { Source = dataContext });
        ratingText.SetBinding(TextBlock.TextProperty, new Binding("Rating") { Source = dataContext });
        grid.Children.Add(ratingText);

        return grid;
    }

    private TextBlock? CreateTitleText(TitleConfig title, object dataContext)
    {
        try
        {
            var textBlock = new TextBlock
            {
                // Use font name directly — WPF's built-in fallback chain handles missing fonts.
                FontFamily = new FontFamily(title.FontFamily),
                Foreground = ParseBrush(title.Foreground),
                Margin = ParseThickness(title.Margin),
                HorizontalAlignment = ParseHorizontalAlignment(title.HorizontalAlignment),
                VerticalAlignment = ParseVerticalAlignment(title.VerticalAlignment)
            };

            textBlock.SetBinding(TextBlock.TextProperty, new Binding("MediaTitle") { Source = dataContext });
            textBlock.SetBinding(VisibilityProperty, new Binding("MockupVisibility") { Source = dataContext });

            textBlock.TextWrapping = title.Wrapping switch
            {
                "Wrap" => TextWrapping.Wrap,
                "WrapWithOverflow" => TextWrapping.WrapWithOverflow,
                _ => TextWrapping.NoWrap
            };

            textBlock.TextTrimming = title.Trimming switch
            {
                "WordEllipsis" => TextTrimming.WordEllipsis,
                "CharacterEllipsis" => TextTrimming.CharacterEllipsis,
                _ => TextTrimming.None
            };

            if (Math.Abs(title.RotationAngle) > 0.01)
            {
                var origin = ParsePoint(title.RotationOrigin);
                textBlock.RenderTransformOrigin = origin;
                // Use full TransformGroup matching original XAML structure:
                // ScaleTransform + SkewTransform + RotateTransform + TranslateTransform
                var transformGroup = new TransformGroup();
                transformGroup.Children.Add(new ScaleTransform());
                transformGroup.Children.Add(new SkewTransform());
                transformGroup.Children.Add(new RotateTransform(title.RotationAngle));
                transformGroup.Children.Add(new TranslateTransform());
                textBlock.RenderTransform = transformGroup;
            }

            return textBlock;
        }
        catch (Exception ex)
        {
            Logger.Warn(ex, "Failed to create title text");
            return null;
        }
    }

    private static ImageSource ResolveImageSource(string path)
    {
        if (path.StartsWith("/", StringComparison.Ordinal))
        {
            // Use explicit pack URI with assembly name — works on any thread
            // (relative URIs depend on Application.Current.BaseUri which may not
            //  be available on the StaRenderer's background STA thread).
            var packUri = new Uri($"pack://application:,,,/FoliCon;component{path}", UriKind.Absolute);
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.UriSource = packUri;
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.EndInit();
            bitmap.Freeze();
            return bitmap;
        }

        var fullPath = Path.IsPathRooted(path) ? path : Path.Combine(AppContext.BaseDirectory, path);
        if (File.Exists(fullPath))
        {
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.UriSource = new Uri(fullPath, UriKind.Absolute);
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.EndInit();
            bitmap.Freeze();
            return bitmap;
        }

        var resourcePath = FileUtils.GetResourcePath(path);
        if (File.Exists(resourcePath))
        {
            var bytes = File.ReadAllBytes(resourcePath);
            using var stream = new MemoryStream(bytes);
            return (ImageSource)new ImageSourceConverter().ConvertFrom(stream);
        }

        throw new FileNotFoundException($"Image not found: {path}");
    }

    private ImageSource? GetPosterImageSource()
    {
        if (DataContext is FoliCon.Models.Data.PosterIcon posterIcon)
            return posterIcon.FolderJpg;
        return null;
    }

    #region Parsing Helpers

    private static Thickness ParseThickness(string margin)
    {
        if (string.IsNullOrWhiteSpace(margin))
            return new Thickness(0);

        var parts = margin.Split(',');
        return parts.Length switch
        {
            1 => new Thickness(ParseDouble(parts[0])),
            2 => new Thickness(ParseDouble(parts[0]), ParseDouble(parts[1]),
                               ParseDouble(parts[0]), ParseDouble(parts[1])),
            3 => new Thickness(ParseDouble(parts[0]), ParseDouble(parts[1]),
                               ParseDouble(parts[2]), ParseDouble(parts[1])),
            4 => new Thickness(ParseDouble(parts[0]), ParseDouble(parts[1]),
                               ParseDouble(parts[2]), ParseDouble(parts[3])),
            _ => new Thickness(0)
        };
    }

    private static double ParseDouble(string value) =>
        double.TryParse(value.Trim(), CultureInfo.InvariantCulture, out var result) ? result : 0;

    private static CornerRadius ParseCornerRadius(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return new CornerRadius(0);

        var parts = value.Split(',');
        return parts.Length switch
        {
            1 => new CornerRadius(ParseDouble(parts[0])),
            4 => new CornerRadius(ParseDouble(parts[0]), ParseDouble(parts[1]),
                                  ParseDouble(parts[2]), ParseDouble(parts[3])),
            _ => new CornerRadius(ParseDouble(parts[0]))
        };
    }

    private static Point ParsePoint(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return new Point(0.5, 0.5);

        var parts = value.Split(',');
        if (parts.Length == 2 &&
            double.TryParse(parts[0].Trim(), CultureInfo.InvariantCulture, out var x) &&
            double.TryParse(parts[1].Trim(), CultureInfo.InvariantCulture, out var y))
            return new Point(x, y);

        return new Point(0.5, 0.5);
    }

    private static Brush ParseBrush(string color)
    {
        try { return (Brush)new BrushConverter().ConvertFromString(color); }
        catch { return Brushes.White; }
    }

    private static HorizontalAlignment ParseHorizontalAlignment(string value) =>
        value?.ToLowerInvariant() switch
        {
            "center" => HorizontalAlignment.Center,
            "right" => HorizontalAlignment.Right,
            "stretch" => HorizontalAlignment.Stretch,
            _ => HorizontalAlignment.Left
        };

    private static VerticalAlignment ParseVerticalAlignment(string value) =>
        value?.ToLowerInvariant() switch
        {
            "center" => VerticalAlignment.Center,
            "bottom" => VerticalAlignment.Bottom,
            "stretch" => VerticalAlignment.Stretch,
            _ => VerticalAlignment.Top
        };

    #endregion
}
