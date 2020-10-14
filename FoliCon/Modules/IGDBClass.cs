using FoliCon.Modules;
using IGDB;
using IGDB.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace FoliCon.Models
{
    public class IGDBClass
    {
        private readonly DataTable listDataTable;
        private readonly IGDBClient serviceClient;
        private readonly List<ImageToDownload> imgDownloadList;

        /// <summary>
        /// IGDB Helper Class for Working with IGDB API efficiently for thi project.
        /// </summary>
        /// <param name="listDataTable">DataTable that stores all the Picked Results.</param>
        /// <param name="serviceClient">Initlized IDGB Client</param>
        /// <param name="imgDownloadList">List that stores all the images to download.</param>
        public IGDBClass(ref DataTable listDataTable, ref IGDBClient serviceClient, ref List<ImageToDownload> imgDownloadList)
        {
            this.listDataTable = listDataTable ?? throw new ArgumentNullException(nameof(listDataTable));
            this.serviceClient = serviceClient ?? throw new ArgumentNullException(nameof(serviceClient));
            this.imgDownloadList = imgDownloadList ?? throw new ArgumentNullException(nameof(imgDownloadList));
        }

        /// <summary>
        /// Searches IGDB for a specified query.
        /// </summary>
        /// <param name="query">Title to search</param>
        /// <returns>Returns Search result with its Media Type</returns>
        public async Task<ResultResponse> SearchGameAsync(string query)
        {
            System.Diagnostics.Contracts.Contract.Assert(serviceClient != null);
            Game[] r = await serviceClient.QueryAsync<Game>(IGDB.IGDBClient.Endpoints.Games, "search " + "\"" + query + "\"" + "; fields name,first_release_date,total_rating,summary,cover.*;");
            ResultResponse response = new ResultResponse
            {
                MediaType = MediaTypes.Game,
                Result = r
            };
            return response;
        }

        public static ObservableCollection<ListItem> ExtractGameDetailsIntoListItem(Game[] result)
        {
            System.Diagnostics.Contracts.Contract.Requires(result != null);
            ObservableCollection<ListItem> items = new ObservableCollection<ListItem>();
            foreach ((string mediaName, string year, string overview, string poster) in from item in result
                                                                                        let mediaName = item.Name
                                                                                        let year = (item.FirstReleaseDate != null) ? item.FirstReleaseDate.Value.Year.ToString() : ""
                                                                                        let overview = item.Summary
                                                                                        let poster = (item.Cover != null) ? "https://" + ImageHelper.GetImageUrl(item.Cover.Value.ImageId, ImageSize.HD720).Substring(2) : null
                                                                                        select (mediaName, year, overview, poster))
            {
                items.Add(new ListItem(mediaName, year, "", overview, poster));
            }

            return items;
        }

        public void ResultPicked(Game result, string fullFolderPath)
        {
            System.Diagnostics.Contracts.Contract.Requires(result != null);
            if (result.Cover == null)
            {
                throw new Exception("NoPoster");
            }
            string folderName = Path.GetFileName(fullFolderPath);
            var localPosterPath = fullFolderPath + @"\" + folderName + ".png";
            var year = (result.FirstReleaseDate != null) ? result.FirstReleaseDate.Value.Year.ToString() : "";
            var posterUrl = ImageHelper.GetImageUrl(result.Cover.Value.ImageId, ImageSize.HD720);
            Util.AddToPickedListDataTable(listDataTable, localPosterPath, result.Name, "", fullFolderPath, folderName, year);
            ImageToDownload tempImage = new ImageToDownload()
            {
                LocalPath = localPosterPath,
                RemotePath = new Uri("https://" + posterUrl.Substring(2))
            };
            imgDownloadList.Add(tempImage);
        }
    }
}