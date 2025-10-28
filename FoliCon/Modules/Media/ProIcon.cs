namespace FoliCon.Modules.Media;

public class ProIcon
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
    private readonly string _filePath;
    private readonly IconOverlay _iconOverlay;
    private readonly string _rating;
    private readonly string _ratingVisibility;
    private readonly string _mockupVisibility;
    private readonly string _mediaTitle;

    public ProIcon(string filePath)
    {
        _filePath = filePath;
        _iconOverlay = IconOverlay.Legacy;
        _rating = string.Empty;
        _ratingVisibility = "hidden";
        _mockupVisibility = "hidden";
        _mediaTitle = string.Empty;
    }

    public ProIcon(string filePath, IconOverlay iconOverlay, string rating = "", string ratingVisibility = "hidden", string mockupVisibility = "hidden", string mediaTitle = "")
    {
        _filePath = filePath;
        _iconOverlay = iconOverlay;
        _rating = rating;
        _ratingVisibility = ratingVisibility;
        _mockupVisibility = mockupVisibility;
        _mediaTitle = mediaTitle;
    }

    public Bitmap RenderToBitmap()
    {
        Logger.Debug("Rendering icon to bitmap with overlay: {Overlay}", _iconOverlay);

        if (_iconOverlay == IconOverlay.Legacy)
        {
            // Use the original simple resize for Legacy mode
            return PosterIconBase.RenderTargetBitmapTo32BppArgb(AsRenderTargetBitmap());
        }

        // For other overlay types, use the appropriate PosterIcon implementation
        using var task = _iconOverlay switch
        {
            IconOverlay.Alternate => StaTask.Start(() =>
                new PosterIconAlt(new PosterIcon(_filePath, _rating, _ratingVisibility, _mockupVisibility))
                    .RenderToBitmap()),
            IconOverlay.Liaher => StaTask.Start(() =>
                new PosterIconLiaher(new PosterIcon(_filePath, _rating, _ratingVisibility, _mockupVisibility))
                    .RenderToBitmap()),
            IconOverlay.Faelpessoal => StaTask.Start(() => new PosterIconFaelpessoal(new PosterIcon(
                _filePath, _rating,
                _ratingVisibility, _mockupVisibility, _mediaTitle)).RenderToBitmap()),
            IconOverlay.FaelpessoalHorizontal => StaTask.Start(() => new PosterIconFaelpessoalHorizontal(
                new PosterIcon(
                    _filePath, _rating,
                    _ratingVisibility, _mockupVisibility, _mediaTitle)).RenderToBitmap()),
            _ => StaTask.Start(() =>
                new Views.PosterIcon(new PosterIcon(_filePath, _rating, _ratingVisibility, _mockupVisibility))
                    .RenderToBitmap())
        };

        return task.Result;
    }

    private BitmapSource AsRenderTargetBitmap()
    {
        using var img = new Bitmap(_filePath);
        using var icon = new Bitmap(img, 256, 256);
        Logger.Debug("Icon resized to 256x256, filePath: {FilePath}", _filePath);
        return ImageUtils.LoadBitmap(icon);
    }
}
