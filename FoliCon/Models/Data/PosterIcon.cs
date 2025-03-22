using System;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using NLog;
using Prism.Mvvm;

namespace FoliCon.Models.Data;

[Localizable(false)]
public class PosterIcon: BindableBase
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
    public ImageSource FolderJpg { get; set; }

    private string _rating;
    private string _mockupVisibility;
    private string _ratingVisibility;
    private string _mediaTitle;

    public string Rating { 
        get=> _rating;
        set => SetProperty(ref _rating, value); 
     }

    public string RatingVisibility
    {
        get => _ratingVisibility;
        set => SetProperty(ref _ratingVisibility, value);
    }
    public string MockupVisibility
    {
        get => _mockupVisibility;
        set => SetProperty(ref _mockupVisibility, value);
    }
    
    public string MediaTitle
    {
        get => _mediaTitle;
        set => SetProperty(ref _mediaTitle, value);
    }

    public PosterIcon()
    {
        try
        {
            var filePath = FileUtils.GetResourcePath("posterDummy.png");
            var thisMemoryStream = new MemoryStream(File.ReadAllBytes(filePath));
            FolderJpg = (ImageSource)new ImageSourceConverter().ConvertFrom(thisMemoryStream);
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
        var thisMemoryStream = new MemoryStream(File.ReadAllBytes(folderJpgPath));
        FolderJpg = (ImageSource)new ImageSourceConverter().ConvertFrom(thisMemoryStream);
    }
    public PosterIcon(string folderJpgPath, string rating, string ratingVisibility, string mockupVisibility, string mediaTitle):this(folderJpgPath, rating, ratingVisibility, mockupVisibility)
    {
        MediaTitle = mediaTitle;
    }
    
    private ImageSource CreatePlaceholderImage()
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