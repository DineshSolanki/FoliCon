using Brush = System.Windows.Media.Brush;
using Brushes = System.Windows.Media.Brushes;
using Point = System.Windows.Point;
using FontFamily = System.Windows.Media.FontFamily;

#nullable enable
namespace FoliCon.Views;

/// <summary>
/// Generic data-driven poster icon renderer that builds its visual tree
/// from a PosterOverlayDefinition at runtime.
/// </summary>
public partial class DynamicPosterIcon : PosterIconBase
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    private const string shieldImagePath = "/Resources/rating_mockup/shield.png";

    private static readonly string[] DefaultLayerOrder = ["base", "poster", "front", "rating", "title"];

    private readonly string? _overlayFolderPath;

    public DynamicPosterIcon(PosterOverlayDefinition definition, object dataContext)
        : base(dataContext)
    {
        ArgumentNullException.ThrowIfNull(definition);
        InitializeComponent();
        DataContext = dataContext;
        Width = definition.DesignWidth;
        Height = definition.DesignHeight;
        _overlayFolderPath = definition.OverlayFolderPath;
        BuildVisualTree(definition, dataContext);
    }

    private void BuildVisualTree(PosterOverlayDefinition definition, object dataContext)
    {
        var rootMargin = ParseThickness(definition.RootMargin);
        var rootGrid = new Grid { Margin = rootMargin };

        // Cache created elements by key
        var elements = new Dictionary<string, UIElement>();

        AddBaseLayer(definition, dataContext, elements);
        AddPosterLayer(definition, elements);
        AddFrontLayer(definition, dataContext, elements);

        // --- Title and Rating ---
        AddTitleAndRating(definition, dataContext, elements);

        // --- Add children in the order specified by LayerOrder (matches original XAML z-order) ---
        var layerOrder = definition.LayerOrder ?? DefaultLayerOrder;
        var added = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var key in layerOrder)
        {
            if (elements.TryGetValue(key, out var element) && added.Add(key))
            {
                rootGrid.Children.Add(element);
            }
        }

        // An element that exists but is missing from layerOrder would otherwise be built and
        // then silently dropped — e.g. turning on the title of an overlay whose layerOrder
        // predates it. Append such elements in the default order so they still render.
        foreach (var key in DefaultLayerOrder)
        {
            if (elements.TryGetValue(key, out var element) && added.Add(key))
            {
                Logger.Debug("Layer '{Key}' is missing from layerOrder for overlay '{Id}'; appending it.",
                    key, definition.Id);
                rootGrid.Children.Add(element);
            }
        }

        Content = rootGrid;
    }

    private void AddBaseLayer(PosterOverlayDefinition definition, object dataContext, Dictionary<string, UIElement> elements)
    {
        if (definition.BaseLayer != null && !string.IsNullOrEmpty(definition.BaseLayer.ImagePath))
        {
            var baseImage = CreateLayerImage(definition.BaseLayer.ImagePath, definition.BaseLayer.Margin);
            if (baseImage != null)
            {
                baseImage.SetBinding(VisibilityProperty, new Binding("MockupVisibility") { Source = dataContext });
                elements["base"] = baseImage;
            }
        }
    }

    private void AddPosterLayer(PosterOverlayDefinition definition, Dictionary<string, UIElement> elements)
    {
        var posterElement = CreatePosterElement(definition);
        if (posterElement != null)
        {
            elements["poster"] = posterElement;
        }
    }

    private void AddFrontLayer(PosterOverlayDefinition definition, object dataContext, Dictionary<string, UIElement> elements)
    {
        if (definition.FrontLayer != null && !string.IsNullOrEmpty(definition.FrontLayer.ImagePath))
        {
            var frontImage = CreateLayerImage(definition.FrontLayer.ImagePath, definition.FrontLayer.Margin);
            if (frontImage != null)
            {
                frontImage.SetBinding(VisibilityProperty, new Binding("MockupVisibility") { Source = dataContext });
                elements["front"] = frontImage;
            }
        }
    }

    private void AddTitleAndRating(PosterOverlayDefinition definition, object dataContext, Dictionary<string, UIElement> elements)
    {
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
        {
            elements["title"] = titleBlock;
        }
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
        var hasClip = !string.IsNullOrWhiteSpace(definition.Poster.ClipRadius) && definition.Poster.ClipRadius != "0";
        var hasOpacityMask = !string.IsNullOrEmpty(definition.Poster.OpacityMaskPath);

        // 1. Perspective 4-corner mapping (if specified and valid)
        if (!string.IsNullOrWhiteSpace(definition.Poster.PerspectiveCorners) &&
            PerspectiveMeshBuilder.TryParseCorners(definition.Poster.PerspectiveCorners, out var corners))
        {
            var posterSource = GetPosterImageSource() ?? CreatePlaceholderImageSource();
            var viewport = PerspectiveMeshBuilder.CreatePerspectivePosterElement(
                corners, posterSource, definition.DesignWidth, definition.DesignHeight);

            if (viewport != null)
            {
                UIElement resultElement = viewport;

                if (hasOpacityMask)
                {
                    var maskGrid = new Grid();
                    maskGrid.Children.Add(viewport);
                    maskGrid.OpacityMask = new ImageBrush(ResolveImageSource(definition.Poster.OpacityMaskPath!))
                    {
                        Stretch = Stretch.Fill
                    };
                    resultElement = maskGrid;
                }

                return resultElement;
            }
        }

        // 2. Standard 2D poster element
        UIElement? element;
        // No clip and no opacity mask — plain Image with margin (direct child of root Grid)
        if (!hasClip && !hasOpacityMask)
        {
            element = CreatePlainPosterImage(definition);
        }
        else if (!hasClip) // hasOpacityMask must be true here
        {
            element = CreatePosterImageWithOpacityMask(definition);
        }
        else
        {
            // Clip (with optional opacity mask) — wrap in Border.
            element = CreateClippedPosterElement(definition, hasOpacityMask);
        }

        // 3. Apply 2D affine transforms (rotation and skew)
        if (element != null)
        {
            ApplyPosterTransforms(element, definition.Poster);
        }

        return element;
    }

    private static void ApplyPosterTransforms(UIElement element, PosterConfig poster)
    {
        var hasRotation = Math.Abs(poster.RotationAngle) > 0.001;
        var hasSkew = Math.Abs(poster.SkewX) > 0.001 || Math.Abs(poster.SkewY) > 0.001;

        if (!hasRotation && !hasSkew)
        {
            return;
        }

        var transformGroup = new TransformGroup();
        if (hasSkew)
        {
            transformGroup.Children.Add(new SkewTransform(poster.SkewX, poster.SkewY));
        }

        if (hasRotation)
        {
            transformGroup.Children.Add(new RotateTransform(poster.RotationAngle));
        }

        element.RenderTransformOrigin = ParsePoint(poster.RotationOrigin);
        element.RenderTransform = transformGroup;
    }

    private static ImageSource CreatePlaceholderImageSource() =>
        BitmapSource.Create(1, 1, 96, 96, PixelFormats.Bgra32, null, new byte[4], 4);

    private Image CreatePlainPosterImage(PosterOverlayDefinition definition)
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

    private Image CreatePosterImageWithOpacityMask(PosterOverlayDefinition definition)
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

    private Border CreateClippedPosterElement(PosterOverlayDefinition definition, bool hasOpacityMask)
    {
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
        {
            posterImageInner.Margin = ParseThickness(definition.Poster.PosterInnerMargin);
        }

        RenderOptions.SetBitmapScalingMode(posterImageInner, BitmapScalingMode.HighQuality);

        var cornerRadius = ParseCornerRadius(definition.Poster.ClipRadius);
        border.CornerRadius = cornerRadius;

        ApplyClipToBorder(border, definition, cornerRadius);

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

    private static void ApplyClipToBorder(Border border, PosterOverlayDefinition definition, CornerRadius cornerRadius)
    {
        // Use explicit ClipRect if provided, otherwise calculate from margins.
        if (!string.IsNullOrWhiteSpace(definition.Poster.ClipRect))
        {
            var rectParts = definition.Poster.ClipRect.Split(',');
            if (rectParts.Length != 4)
            {
                return;
            }

            var rx = ParseDouble(rectParts[0]);
            var ry = ParseDouble(rectParts[1]);
            var rw = ParseDouble(rectParts[2]);
            var rh = ParseDouble(rectParts[3]);
            border.Clip = new RectangleGeometry(
                new Rect(rx, ry, rw, rh),
                cornerRadius.TopLeft, cornerRadius.TopLeft);
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
            Source = ResolveImageSource(shieldImagePath),
            Margin = ParseThickness(rating.ShieldMargin)
        };
        RenderOptions.SetBitmapScalingMode(shield, BitmapScalingMode.HighQuality);
        shield.SetBinding(VisibilityProperty, new Binding("RatingVisibility") { Source = dataContext });

        var ratingText = new TextBlock
        {
            // Use font name directly — WPF's built-in fallback chain handles missing fonts,
            // matching the original XAML behavior exactly.
            FontFamily = new FontFamily(rating.FontFamily),
            FontStyle = FontStyles.Italic,
            FontSize = rating.FontSize,
            Foreground = Brushes.Black,
            TextWrapping = TextWrapping.NoWrap,
            TextAlignment = TextAlignment.Center,
            // Give the TextBlock the full bounds so text centers within them;
            // the Viewbox then scales the entire block down if the rating is long.
            Width = rating.TextWidth,
            Height = rating.TextHeight
        };
        ratingText.SetBinding(TextBlock.TextProperty, new Binding("Rating") { Source = dataContext });

        // Wrap in a Viewbox so the text scales down to fit when the rating string
        // is wider than the available badge space (e.g. "10.0", "9.15").
        var ratingViewbox = new Viewbox
        {
            Child = ratingText,
            Stretch = Stretch.Uniform,
            StretchDirection = StretchDirection.DownOnly,
            MaxWidth = rating.TextWidth,
            MaxHeight = rating.TextHeight,
            Margin = ParseThickness(rating.TextMargin)
        };
        ratingViewbox.SetBinding(VisibilityProperty, new Binding("RatingVisibility") { Source = dataContext });

        if (string.Equals(rating.TextAnchor, "Center", StringComparison.OrdinalIgnoreCase))
        {
            // Co-locate shield and text in a nested Grid so the text auto-centers
            // on the shield regardless of image aspect ratio or size.
            // TextMargin becomes an offset from the shield center.
            var badgeGrid = new Grid
            {
                Margin = ParseThickness(rating.ShieldMargin)
            };
            Panel.SetZIndex(badgeGrid, 2);
            Grid.SetRow(badgeGrid, 1);
            Grid.SetRowSpan(badgeGrid, 2);

            // Shield fills the badge grid
            shield.Margin = new Thickness(0);
            badgeGrid.Children.Add(shield);

            // Text is centered on the shield with optional offset
            ratingViewbox.Margin = ParseThickness(rating.TextMargin);
            ratingViewbox.HorizontalAlignment = ParseHorizontalAlignment(rating.TextHorizontalAlignment);
            ratingViewbox.VerticalAlignment = ParseVerticalAlignment(rating.TextVerticalAlignment);
            Panel.SetZIndex(ratingViewbox, 3);
            badgeGrid.Children.Add(ratingViewbox);

            grid.Children.Add(badgeGrid);
        }
        else
        {
            // Legacy behavior: shield and text positioned independently via absolute margins.
            Panel.SetZIndex(shield, 2);
            Grid.SetRow(shield, 1);
            Grid.SetRowSpan(shield, 2);
            grid.Children.Add(shield);

            Panel.SetZIndex(ratingViewbox, 3);
            Grid.SetRow(ratingViewbox, 2);
            ratingViewbox.HorizontalAlignment = ParseHorizontalAlignment(rating.TextHorizontalAlignment);
            ratingViewbox.VerticalAlignment = ParseVerticalAlignment(rating.TextVerticalAlignment);
            grid.Children.Add(ratingViewbox);
        }

        return grid;
    }

    private static TextBlock? CreateTitleText(TitleConfig title, object dataContext)
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

    private ImageSource ResolveImageSource(string path)
    {
        if (path.StartsWith('/'))
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

        // Resolve against the overlay's folder (community overlays use relative paths)
        if (_overlayFolderPath != null)
        {
            var overlayPath = Path.Combine(_overlayFolderPath, path);
            if (File.Exists(overlayPath))
            {
                return LoadBitmapFromPath(overlayPath);
            }
        }

        var fullPath = Path.IsPathRooted(path) ? path : Path.Combine(AppContext.BaseDirectory, path);
        if (File.Exists(fullPath))
        {
            return LoadBitmapFromPath(fullPath);
        }

        var resourcePath = FileUtils.GetResourcePath(path);
        if (!File.Exists(resourcePath))
        {
            throw new FileNotFoundException($"Image not found: {path}");
        }

        var bytes = File.ReadAllBytes(resourcePath);
        using var stream = new MemoryStream(bytes);
        return (ImageSource)new ImageSourceConverter().ConvertFrom(stream)!;

    }

    private static BitmapImage LoadBitmapFromPath(string fullPath)
    {
        var bytes = File.ReadAllBytes(fullPath);
        var bitmap = new BitmapImage();
        bitmap.BeginInit();
        bitmap.CacheOption = BitmapCacheOption.OnLoad;
        bitmap.StreamSource = new MemoryStream(bytes);
        bitmap.EndInit();
        bitmap.Freeze();
        return bitmap;
    }

    private ImageSource? GetPosterImageSource()
    {
        if (DataContext is FoliCon.Models.Data.PosterIcon posterIcon)
        {
            return posterIcon.FolderJpg;
        }

        return null;
    }

    #region Parsing Helpers

    private static Thickness ParseThickness(string margin)
    {
        if (string.IsNullOrWhiteSpace(margin))
        {
            return new Thickness(0);
        }

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
        {
            return new CornerRadius(0);
        }

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
        {
            return new Point(0.5, 0.5);
        }

        var parts = value.Split(',');
        if (parts.Length == 2 &&
            double.TryParse(parts[0].Trim(), CultureInfo.InvariantCulture, out var x) &&
            double.TryParse(parts[1].Trim(), CultureInfo.InvariantCulture, out var y))
        {
            return new Point(x, y);
        }

        return new Point(0.5, 0.5);
    }

    private static Brush ParseBrush(string color)
    {
        try
        {
            return (Brush)new BrushConverter().ConvertFromString(color)!;
        }
        catch
        {
            return Brushes.White;
        }
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
