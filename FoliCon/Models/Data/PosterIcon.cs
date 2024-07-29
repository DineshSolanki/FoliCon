namespace FoliCon.Models.Data;

[Localizable(false)]
public class PosterIcon: BindableBase
{
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
        var filePath = $"{Path.GetTempPath()}\\posterDummy.png";
        if (!FileUtils.FileExists(filePath))
        {
            _ = NetworkUtils.DownloadImageFromUrlAsync(new Uri("https://image.tmdb.org/t/p/original/r0bgHi3MwGHTKPWyJdORsb4ukY8.jpg"), filePath);
        }
        var thisMemoryStream = new MemoryStream(File.ReadAllBytes(filePath));
        FolderJpg = (ImageSource)new ImageSourceConverter().ConvertFrom(thisMemoryStream);
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
}