using FoliCon.Modules.utils;

namespace FoliCon.Models.Data;

public class PosterIcon: BindableBase
{
    public ImageSource FolderJpg { get; set; }
    public string RatingVisibility { get; set; }
    private string _rating;
    public string Rating { 
        get=> _rating;
        set => SetProperty(ref _rating, value); 
     }
    public string MockupVisibility { get; set; }
    public string MediaTitle { get; set; }

    public PosterIcon()
    {
        var filePath = Path.GetTempPath() + "\\posterDummy.png";
        if (!File.Exists(filePath))
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
        //byte[] bytes = Util.ImageSourceToBytes(new PngBitmapEncoder(), FolderJpg);
        //string base64string = System.Convert.ToBase64String(bytes);
    }
    public PosterIcon(string folderJpgPath, string rating, string ratingVisibility, string mockupVisibility, string mediaTitle="FoliCon")
    {
        RatingVisibility = ratingVisibility;
        Rating = rating;
        MockupVisibility = mockupVisibility;
        var thisMemoryStream = new MemoryStream(File.ReadAllBytes(folderJpgPath));
        FolderJpg = (ImageSource)new ImageSourceConverter().ConvertFrom(thisMemoryStream);
        MediaTitle = mediaTitle;
        //byte[] bytes = Util.ImageSourceToBytes(new PngBitmapEncoder(), FolderJpg);
        //string base64string = System.Convert.ToBase64String(bytes);
    }
}