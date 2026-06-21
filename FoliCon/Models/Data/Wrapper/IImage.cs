namespace FoliCon.Models.Data.Wrapper
{
    public interface IImage
    {
        string GetThumbnailUrl();
        string GetPosterUrl();
        string Id { get; }
    }

    public class ArtworkWrapper(Artwork artwork) : IImage
    {
        public string GetThumbnailUrl() => IgdbDataTransformer.GetPosterUrl(artwork.ImageId, ImageSize.ScreenshotMed);

        public string GetPosterUrl() => IgdbDataTransformer.GetPosterUrl(artwork.ImageId, ImageSize.HD720);

        public string Id => artwork.ImageId;
    }

    public class ImageDataWrapper(int id, ImageData imageData, TMDbClient client) : IImage
    {
        public string GetThumbnailUrl() => TmdbDataTransformer.GetPosterUrl(imageData, PosterSize.W92, client);

        public string GetPosterUrl() => TmdbDataTransformer.GetPosterUrl(imageData, PosterSize.W500, client);

        public string Id => id.ToString();
    }
}