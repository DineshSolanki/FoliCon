using FoliCon.Models.Constants;
using FoliCon.Models.Data;
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
        if (_serviceClient is null) throw new InvalidOperationException("Service Client is not initialized.");

        var r = await _serviceClient.QueryAsync<Game>(IGDBClient.Endpoints.Games,
            $"search \"{query}\"; fields artworks.image_id, name,first_release_date,total_rating,summary,cover.*;");

        return new ResultResponse
        {
            MediaType = MediaTypes.Game,
            Result = r
        };
    }

    public Task<GameVideo[]> GetGameVideo(string id)
    {
        Logger.Debug("Searching IGDB video by title id: {Id}", id);
        if (_serviceClient is null) throw new InvalidOperationException("Service Client is not initialized.");

        return _serviceClient.QueryAsync<GameVideo>(IGDBClient.Endpoints.GameVideos,
            $"fields game,name,video_id; where game = {id};");
    }

    public async Task<ResultResponse> SearchGameByIdAsync(string id)
    {
        Logger.Debug("Searching IGDB for {Id}", id);
        if (_serviceClient is null) throw new InvalidOperationException("Service Client is not initialized.");

        var result = await _serviceClient.QueryAsync<Game>(IGDBClient.Endpoints.Games,
            $"fields artworks.image_id, name,first_release_date,total_rating,summary,cover.*; where id = {id};");

        return new ResultResponse
        {
            MediaType = MediaTypes.Game,
            Result = result
        };
    }

    public Task<Artwork[]> GetArtworksByGameIdAsync(string id)
    {
        if (_serviceClient is null) throw new InvalidOperationException("Service Client is not initialized.");

        return _serviceClient.QueryAsync<Artwork>(IGDBClient.Endpoints.Artworks,
            $"fields image_id; where game = {id};");
    }
}