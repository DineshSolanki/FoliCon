using NLog;

namespace FoliCon.Modules.IGDB;

public class IgdbService(ref IGDBClient serviceClient)
{
    private static readonly NLog.Logger Logger = LogManager.GetCurrentClassLogger();
    private readonly IGDBClient _serviceClient = serviceClient ?? throw new ArgumentNullException(nameof(serviceClient));

    public IGDBClient GetClient()
    {
        return _serviceClient;
    }
    public async Task<ResultResponse> SearchGameAsync(string query)
    {
        Logger.Debug("Searching IGDB for {Query}", query);
        Contract.Assert(_serviceClient != null);
        var r = await _serviceClient.QueryAsync<Game>(IGDBClient.Endpoints.Games,
            $"search \"{query}\"; fields artworks.image_id, name,first_release_date,total_rating,summary,cover.*;");

        var response = new ResultResponse
        {
            MediaType = MediaTypes.Game,
            Result = r
        };
        return response;
    }
    
    public async Task<GameVideo[]> GetGameVideo(string id)
    {
        Logger.Debug("Searching IGDB video by title id: {Id}", id);
        Contract.Assert(_serviceClient != null);
        var r = await _serviceClient.QueryAsync<GameVideo>(IGDBClient.Endpoints.GameVideos,
            $"fields game,name,video_id; where game = {id};");
        return r;
    }
    
    public async Task<ResultResponse> SearchGameByIdAsync(string id)
    {
        Logger.Debug("Searching IGDB for {Id}", id);
        Contract.Assert(_serviceClient != null);
        var r = await _serviceClient.QueryAsync<Game>(IGDBClient.Endpoints.Games,
            $"fields artworks.image_id, name,first_release_date,total_rating,summary,cover.*; where id = {id};");
        var response = new ResultResponse
        {
            MediaType = MediaTypes.Game,
            Result = r
        };
        return response;
    }
    
    public async Task<Artwork[]> GetArtworksByGameIdAsync(string id)
    {
        var r = await _serviceClient.QueryAsync<Artwork>(IGDBClient.Endpoints.Artworks,
            $"fields image_id; where game = {id};");
        return r;
    }
}