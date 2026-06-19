namespace FoliCon.Models.Data;

public class DArtImageList : BindableBase
{
    public DArtImageList(string url, string thumbnailUrl)
    {
        Url = url ?? throw new ArgumentNullException(nameof(url));
        ThumbnailUrl = thumbnailUrl ?? throw new ArgumentNullException(nameof(thumbnailUrl));
    }

    public DArtImageList(string url, string thumbnailUrl, string deviationId) : this(url, thumbnailUrl)
    {
        DeviationId = deviationId ?? throw new ArgumentNullException(nameof(deviationId));
    }

    public DArtImageList(string url, string thumbnailUrl, string deviationId,
        bool requiresWatch, string authorUsername)
        : this(url, thumbnailUrl, deviationId)
    {
        RequiresWatch = requiresWatch;
        AuthorUsername = authorUsername;
    }

    public string Url
    {
        get;
        set => SetProperty(ref field, value);
    }

    public string ThumbnailUrl
    {
        get;
        set => SetProperty(ref field, value);
    }

    public string DeviationId
    {
        get;
        set => SetProperty(ref field, value);
    }

    public bool RequiresWatch
    {
        get;
        set => SetProperty(ref field, value);
    }

    public string AuthorUsername
    {
        get;
        set => SetProperty(ref field, value);
    }
}
