using FoliCon.Models.Constants;
using FoliCon.Modules.IGDB;
using FoliCon.Modules.TMDB;

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
        public string GetThumbnailUrl()
        {
            return IgdbDataTransformer.GetPosterUrl(artwork.ImageId, ImageSize.ScreenshotMed);
        }

        public string GetPosterUrl()
        {
            return IgdbDataTransformer.GetPosterUrl(artwork.ImageId, ImageSize.HD720);
        }

        public string Id => artwork.ImageId;
    }

    public class ImageDataWrapper(int id, ImageData imageData, TMDbClient client) : IImage
    {
        public string GetThumbnailUrl()
        {
            return TmdbDataTransformer.GetPosterUrl(imageData, PosterSize.W92, client);
        }

        public string GetPosterUrl()
        {
            return TmdbDataTransformer.GetPosterUrl(imageData, PosterSize.W500, client);
        }

        public string Id => id.ToString();
    }
}