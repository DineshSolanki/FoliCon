namespace FoliCon.Models.Data;

public class DArtImageList : BindableBase
{
    private string _url;
    private BitmapSource _image;
    private string _deviationId;
    private bool _mustWatch;

    public DArtImageList(string url, BitmapSource bmp)
    {
        Url = url ?? throw new ArgumentNullException(nameof(url));
        Image = bmp ?? throw new ArgumentNullException(nameof(bmp));
    }

    public DArtImageList(string url, BitmapSource bmp, string deviationId, bool mustWatch) : this(url, bmp)
    {
        DeviationId = deviationId ?? throw new ArgumentNullException(nameof(deviationId));
        MustWatch = mustWatch;
    }
    
    public string Url { get => _url; set => SetProperty(ref _url, value); }
    public BitmapSource Image { get => _image; set => SetProperty(ref _image, value); }
    public string DeviationId { get => _deviationId; set => SetProperty(ref _deviationId, value); }
    public bool MustWatch { get => _mustWatch; set => SetProperty(ref _mustWatch, value); }
}