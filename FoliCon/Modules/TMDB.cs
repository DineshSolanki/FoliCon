using FoliCon.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using TMDbLib.Client;
using TMDbLib.Objects.General;
using TMDbLib.Objects.Search;

namespace FoliCon.Modules
{
    public class Tmdb
    {
        private const string PosterBase = "http://image.tmdb.org/t/p/original";
        private const string SmallPosterBase = "https://image.tmdb.org/t/p/w200";
        private readonly List<ImageToDownload> _imgDownloadList;
        private readonly DataTable _listDataTable;
        private readonly TMDbClient _serviceClient;

        /// <summary>
        /// TMDB Helper Class for Working with IGDB API efficiently for this project.
        /// </summary>
        /// <param name="serviceClient">Initialized TMDB Client</param>
        /// <param name="listDataTable">DataTable that stores all the Picked Results.</param>
        /// <param name="imgDownloadList">List that stores all the images to download.</param>
        public Tmdb(ref TMDbClient serviceClient, ref DataTable listDataTable,
            ref List<ImageToDownload> imgDownloadList)
        {
            _listDataTable = listDataTable;
            _serviceClient = serviceClient;
            _imgDownloadList = imgDownloadList;
        }

        public static ObservableCollection<ListItem> ExtractCollectionDetailsIntoListItem(
            SearchContainer<SearchCollection> result)
        {
            var items = new ObservableCollection<ListItem>();
            foreach (var item in result.Results)
            {
                var mediaName = item.Name;
                const string year = "";
                const string rating = "";
                const string overview = "";
                var poster = Convert.ToString(item.PosterPath != null ? SmallPosterBase + item.PosterPath : null, CultureInfo.InvariantCulture);
                items.Add(new ListItem(mediaName, year, rating, overview, poster));
            }

            return items;
        }

        public static ObservableCollection<ListItem> ExtractMoviesDetailsIntoListItem(
            SearchContainer<SearchMovie> result)
        {
            var items = new ObservableCollection<ListItem>();
            foreach (var item in result.Results)
            {
                var mediaName = item.Title;
                var year = item.ReleaseDate != null ? item.ReleaseDate.Value.Year.ToString() : "";
                var rating = item.VoteAverage.ToString(CultureInfo.CurrentCulture);
                var overview = item.Overview;
                var poster = item.PosterPath != null ? SmallPosterBase + item.PosterPath : null;
                items.Add(new ListItem(mediaName, year, rating, overview, poster));
            }

            return items;
        }

        public static ObservableCollection<ListItem> ExtractResourceDetailsIntoListItem(
            SearchContainer<SearchBase> result)
        {
            var items = new ObservableCollection<ListItem>();
            var mediaName = "";
            var year = "";
            var rating = "";
            var overview = "";
            string poster = null;
            foreach (var item in result.Results)
            {
                var mediaType = item.MediaType;
                switch (mediaType)
                {
                    case MediaType.Tv:
                        {
                            var res = (SearchTv)item;
                            mediaName = res.Name;
                            year = res.FirstAirDate != null ? res.FirstAirDate.Value.Year.ToString() : "";
                            rating = res.VoteAverage.ToString(CultureInfo.CurrentCulture);
                            overview = res.Overview;
                            poster = res.PosterPath != null ? SmallPosterBase + res.PosterPath : null;
                            break;
                        }
                    case MediaType.Movie:
                        {
                            var res = (SearchMovie)item;
                            mediaName = res.Title;
                            year = res.ReleaseDate != null ? res.ReleaseDate.Value.Year.ToString() : "";
                            rating = res.VoteAverage.ToString(CultureInfo.CurrentCulture);
                            overview = res.Overview;
                            poster = res.PosterPath != null ? SmallPosterBase + res.PosterPath : null;
                            break;
                        }
                }

                items.Add(new ListItem(mediaName, year, rating, overview, poster));
            }

            return items;
        }

        public static ObservableCollection<ListItem> ExtractTvDetailsIntoListItem(SearchContainer<SearchTv> result)
        {
            var items = new ObservableCollection<ListItem>();
            foreach (var item in result.Results)
            {
                var mediaName = item.Name;
                var year = item.FirstAirDate != null ? item.FirstAirDate.Value.Year.ToString() : "";
                var rating = item.VoteAverage.ToString(CultureInfo.CurrentCulture);
                var overview = item.Overview;
                var poster = item.PosterPath != null ? SmallPosterBase + item.PosterPath : null;
                items.Add(new ListItem(mediaName, year, rating, overview, poster));
            }

            return items;
        }

        /// <summary>
        /// Prepares the Selected Result for Download And final List View
        /// </summary>
        /// <param name="result">Search Response</param>
        /// <param name="resultType">Type of search Response.</param>
        /// <param name="fullFolderPath">Full Path to the current Media Folder</param>
        /// TODO: Merge parameter response and resultType.
        public void ResultPicked(dynamic result, string resultType, string fullFolderPath)
        {
            if (result.PosterPath == null)
            {
                throw new InvalidDataException("NoPoster");
            }

            var folderName = Path.GetFileName(fullFolderPath);
            var localPosterPath = fullFolderPath + @"\" + folderName + ".png";
            string posterUrl = string.Concat(PosterBase, result.PosterPath);
            if (resultType == MediaTypes.Tv)
            {
                var pickedResult = (SearchTv)result;
                var year = pickedResult.FirstAirDate != null ? pickedResult.FirstAirDate.Value.Year.ToString() : "";
                Util.AddToPickedListDataTable(_listDataTable, localPosterPath, pickedResult.Name,
                    pickedResult.VoteAverage.ToString(CultureInfo.CurrentCulture), fullFolderPath, folderName,
                    year);
            }
            else if (resultType == MediaTypes.Movie)
            {
                var pickedResult = (SearchMovie)result;
                var year = pickedResult.ReleaseDate != null ? pickedResult.ReleaseDate.Value.Year.ToString() : "";
                Util.AddToPickedListDataTable(_listDataTable, localPosterPath, pickedResult.Title,
                    pickedResult.VoteAverage.ToString(CultureInfo.CurrentCulture), fullFolderPath, folderName, year);
            }
            else if (resultType == MediaTypes.Collection)
            {
                var searchResult = (SearchCollection)result;
                Util.AddToPickedListDataTable(_listDataTable, localPosterPath, searchResult.Name, "", fullFolderPath,
                    folderName);
            }
            else if (resultType == MediaTypes.Mtv)
            {
                MediaType mediaType = result.MediaType;
                switch (mediaType)
                {
                    case MediaType.Tv:
                        {
                            SearchTv pickedResult = result;
                            var year = pickedResult.FirstAirDate != null
                                ? pickedResult.FirstAirDate.Value.Year.ToString()
                                : "";
                            Util.AddToPickedListDataTable(_listDataTable, localPosterPath, pickedResult.Name,
                                pickedResult.VoteAverage.ToString(CultureInfo.CurrentCulture), fullFolderPath, folderName,
                                year);
                            break;
                        }
                    case MediaType.Movie:
                        {
                            SearchMovie pickedResult = result;
                            var year = pickedResult.ReleaseDate != null
                                ? pickedResult.ReleaseDate.Value.Year.ToString()
                                : "";
                            Util.AddToPickedListDataTable(_listDataTable, localPosterPath, pickedResult.Title,
                                pickedResult.VoteAverage.ToString(CultureInfo.CurrentCulture), fullFolderPath, folderName,
                                year);
                            break;
                        }
                }
            }

            var tempImage = new ImageToDownload
            {
                LocalPath = localPosterPath,
                RemotePath = new Uri(posterUrl)
            };
            _imgDownloadList.Add(tempImage);
        }

        /// <summary>
        /// Searches TMDB for a query in Specified search mode
        /// </summary>
        /// <param name="query">Title to search</param>
        /// <param name="searchMode">Search Mode such as Movie,TV</param>
        /// <returns>Returns Search result with its Media Type</returns>
        public async Task<ResultResponse> SearchAsync(string query, string searchMode)
        {
            object r = null;
            var mediaType = "";
            if (searchMode == MediaTypes.Movie)
            {
                if (query.ToLower(CultureInfo.InvariantCulture).Contains("collection"))
                {
                    r = _serviceClient.SearchCollectionAsync(query).Result;
                    mediaType = MediaTypes.Collection;
                }
                else
                {
                    r = await _serviceClient.SearchMovieAsync(query);
                    mediaType = MediaTypes.Movie;
                }
            }
            else if (searchMode == MediaTypes.Tv)
            {
                r = _serviceClient.SearchTvShowAsync(query).Result;
                mediaType = MediaTypes.Tv;
            }
            else if (searchMode == MediaTypes.Mtv)
            {
                r = _serviceClient.SearchMultiAsync(query).Result;
                mediaType = MediaTypes.Mtv;
            }

            var response = new ResultResponse
            {
                Result = r,
                MediaType = mediaType
            };
            return response;
        }
    }
}