namespace FoliCon.Models.Data;

public class DArtImageList : BindableBase
{
    private string _url;
    private string  _thumbnailUrl;
    private string _deviationId;

    public DArtImageList(string url, string thumbnailUrl)
    {
        Url = url ?? throw new ArgumentNullException(nameof(url));
        ThumbnailUrl = thumbnailUrl ?? throw new ArgumentNullException(nameof(thumbnailUrl));
    }

    public DArtImageList(string url, string thumbnailUrl, string deviationId) : this(url, thumbnailUrl)
    {
        DeviationId = deviationId ?? throw new ArgumentNullException(nameof(deviationId));
    }
    
    public string Url { get => _url; set => SetProperty(ref _url, value); }
    public string ThumbnailUrl { get => _thumbnailUrl; set => SetProperty(ref _thumbnailUrl, value); }
    public string DeviationId { get => _deviationId; set => SetProperty(ref _deviationId, value); }
}