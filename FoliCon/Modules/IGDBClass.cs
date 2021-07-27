using FoliCon.Models;
using IGDB;
using IGDB.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace FoliCon.Modules
{
    public class IgdbClass
    {
        private readonly DataTable _listDataTable;
        private readonly IGDBClient _serviceClient;
        private readonly List<ImageToDownload> _imgDownloadList;

        /// <summary>
        /// IGDB Helper Class for Working with IGDB API efficiently for thi project.
        /// </summary>
        /// <param name="listDataTable">DataTable that stores all the Picked Results.</param>
        /// <param name="serviceClient">Initialized IGDB/Twitch Client</param>
        /// <param name="imgDownloadList">List that stores all the images to download.</param>
        public IgdbClass(ref DataTable listDataTable, ref IGDBClient serviceClient,
            ref List<ImageToDownload> imgDownloadList)
        {
            _listDataTable = listDataTable ?? throw new ArgumentNullException(nameof(listDataTable));
            _serviceClient = serviceClient ?? throw new ArgumentNullException(nameof(serviceClient));
            _imgDownloadList = imgDownloadList ?? throw new ArgumentNullException(nameof(imgDownloadList));
        }

        /// <summary>
        /// Searches IGDB for a specified query.
        /// </summary>
        /// <param name="query">Title to search</param>
        /// <returns>Returns Search result with its Media Type</returns>
        public async Task<ResultResponse> SearchGameAsync(string query)
        {
            Contract.Assert(_serviceClient != null);
            var r = await _serviceClient.QueryAsync<Game>(IGDBClient.Endpoints.Games,
                $"search \"{query}\"; fields name,first_release_date,total_rating,summary,cover.*;");
            var response = new ResultResponse
            {
                MediaType = MediaTypes.Game,
                Result = r
            };
            return response;
        }

        public static ObservableCollection<ListItem> ExtractGameDetailsIntoListItem(Game[] result)
        {
            var items = new ObservableCollection<ListItem>();
            foreach (var (mediaName, year, overview, poster) in from item in result
                                                                let mediaName = item.Name
                                                                let year = item.FirstReleaseDate != null ? item.FirstReleaseDate.Value.Year.ToString() : ""
                                                                let overview = item.Summary
                                                                let poster = item.Cover != null
                                                                    ? "https://" + ImageHelper.GetImageUrl(item.Cover.Value.ImageId, ImageSize.HD720)[2..]
                                                                    : null
                                                                select (mediaName, year, overview, poster))
            {
                items.Add(new ListItem(mediaName, year, "", overview, poster));
            }

            return items;
        }

        public void ResultPicked(Game result, string fullFolderPath, string rating = "")
        {
            if (result.Cover == null)
            {
                throw new InvalidDataException("NoPoster");
            }

            var folderName = Path.GetFileName(fullFolderPath);
            var localPosterPath = fullFolderPath + @"\" + folderName + ".png";
            var year = result.FirstReleaseDate != null ? result.FirstReleaseDate.Value.Year.ToString() : "";
            var posterUrl = ImageHelper.GetImageUrl(result.Cover.Value.ImageId, ImageSize.HD720);
            Util.AddToPickedListDataTable(_listDataTable, localPosterPath, result.Name, rating, fullFolderPath, folderName,
                year);
            var tempImage = new ImageToDownload
            {
                LocalPath = localPosterPath,
                RemotePath = new Uri("https://" + posterUrl[2..])
            };
            _imgDownloadList.Add(tempImage);
        }
    }
}