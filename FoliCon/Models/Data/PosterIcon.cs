using Brushes = System.Windows.Media.Brushes;
using Point = System.Windows.Point;
using Size = System.Windows.Size;

namespace FoliCon.Models.Data;

[Localizable(false)]
public class PosterIcon : BindableBase
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
    public ImageSource FolderJpg { get; set; }

    public string Rating
    {
        get;
        set => SetProperty(ref field, value);
    }

    public string RatingVisibility
    {
        get;
        set => SetProperty(ref field, value);
    }

    public string MockupVisibility
    {
        get;
        set => SetProperty(ref field, value);
    }

    public string MediaTitle
    {
        get;
        set => SetProperty(ref field, value);
    }

    public PosterIcon()
    {
        try
        {
            var filePath = FileUtils.GetResourcePath("posterDummy.png");
            using var memoryStream = new MemoryStream(File.ReadAllBytes(filePath));
            FolderJpg = (ImageSource)new ImageSourceConverter().ConvertFrom(memoryStream);
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to load poster image. Using default placeholder.");
            FolderJpg = CreatePlaceholderImage();
        }

        Rating = "7.8";
        MockupVisibility = "visible";
        RatingVisibility = "visible";
        MediaTitle = "Made with ♥ by FoliCon";
    }

    public PosterIcon(string folderJpgPath, string rating, string ratingVisibility, string mockupVisibility)
    {
        RatingVisibility = ratingVisibility;
        Rating = rating;
        MockupVisibility = mockupVisibility;
        using var memoryStream = new MemoryStream(File.ReadAllBytes(folderJpgPath));
        FolderJpg = (ImageSource)new ImageSourceConverter().ConvertFrom(memoryStream);
    }

    public PosterIcon(string folderJpgPath, string rating, string ratingVisibility, string mockupVisibility,
        string mediaTitle) : this(folderJpgPath, rating, ratingVisibility, mockupVisibility)
    {
        MediaTitle = mediaTitle;
    }

    private static ImageSource CreatePlaceholderImage()
    {
        // Create a simple colored rectangle as fallback
        var drawingVisual = new DrawingVisual();
        using (var drawingContext = drawingVisual.RenderOpen())
        {
            drawingContext.DrawRectangle(
                Brushes.Gray,
                null,
                new Rect(new Point(0, 0), new Size(300, 450)));
        }

        var renderTargetBitmap = new RenderTargetBitmap(
            300, 450, 96, 96, PixelFormats.Pbgra32);
        renderTargetBitmap.Render(drawingVisual);
        return renderTargetBitmap;
    }
}