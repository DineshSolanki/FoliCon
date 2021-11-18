namespace FoliCon.Models;

public class DArtImageList : BindableBase
{
    private string _url;
    private BitmapSource _image;

    public DArtImageList(string url, BitmapSource bmp)
    {
        Url = url ?? throw new ArgumentNullException(nameof(url));
        Image = bmp ?? throw new ArgumentNullException(nameof(bmp));
    }

    public string Url { get => _url; set => SetProperty(ref _url, value); }
    public BitmapSource Image { get => _image; set => SetProperty(ref _image, value); }
}