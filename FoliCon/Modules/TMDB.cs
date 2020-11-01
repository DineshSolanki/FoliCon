﻿using FoliCon.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
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
        private readonly TMDbLib.Client.TMDbClient _serviceClient;

        /// <summary>
        /// TMDB Helper Class for Working with IGDB API efficiently for this project.
        /// </summary>
        /// <param name="serviceClient">Initialized TMDB Client</param>
        /// <param name="listDataTable">DataTable that stores all the Picked Results.</param>
        /// <param name="imgDownloadList">List that stores all the images to download.</param>
        public Tmdb(ref TMDbLib.Client.TMDbClient serviceClient, ref DataTable listDataTable,
            ref List<ImageToDownload> imgDownloadList)
        {
            _listDataTable = listDataTable;
            _serviceClient = serviceClient;
            _imgDownloadList = imgDownloadList;
        }

        public static ObservableCollection<ListItem> ExtractCollectionDetailsIntoListItem(
            SearchContainer<SearchCollection> result)
        {
            ObservableCollection<ListItem> items = new ObservableCollection<ListItem>();
            foreach (var item in result.Results)
            {
                string mediaName = item.Name;
                string year = "";
                string rating = "";
                string overview = "";
                string poster = Convert.ToString((item.PosterPath != null) ? SmallPosterBase + item.PosterPath : null);
                items.Add(new ListItem(mediaName, year, rating, overview, poster));
            }

            return items;
        }

        public static ObservableCollection<ListItem> ExtractMoviesDetailsIntoListItem(
            SearchContainer<SearchMovie> result)
        {
            ObservableCollection<ListItem> items = new ObservableCollection<ListItem>();
            foreach (var item in result.Results)
            {
                string mediaName = item.Title;
                string year = item.ReleaseDate != null ? item.ReleaseDate.Value.Year.ToString() : "";
                string rating = item.VoteAverage.ToString(CultureInfo.CurrentCulture);
                string overview = item.Overview;
                string poster = (item.PosterPath != null) ? SmallPosterBase + item.PosterPath : null;
                items.Add(new ListItem(mediaName, year, rating, overview, poster));
            }

            return items;
        }

        public static ObservableCollection<ListItem> ExtractResourceDetailsIntoListItem(
            SearchContainer<SearchBase> result)
        {
            ObservableCollection<ListItem> items = new ObservableCollection<ListItem>();
            string mediaName = "";
            string year = "";
            string rating = "";
            string overview = "";
            string poster = null;
            foreach (var item in result.Results)
            {
                var mediaType = item.MediaType;
                switch (mediaType)
                {
                    case MediaType.Tv:
                    {
                        SearchTv res = (SearchTv) item;
                        mediaName = res.Name;
                        year = (res.FirstAirDate != null) ? res.FirstAirDate.Value.Year.ToString() : "";
                        rating = res.VoteAverage.ToString(CultureInfo.CurrentCulture);
                        overview = res.Overview;
                        poster = (res.PosterPath != null) ? SmallPosterBase + res.PosterPath : null;
                        break;
                    }
                    case MediaType.Movie:
                    {
                        SearchMovie res = (SearchMovie) item;
                        mediaName = res.Title;
                        year = (res.ReleaseDate != null) ? res.ReleaseDate.Value.Year.ToString() : "";
                        rating = res.VoteAverage.ToString(CultureInfo.CurrentCulture);
                        overview = res.Overview;
                        poster = (res.PosterPath != null) ? SmallPosterBase + res.PosterPath : null;
                        break;
                    }
                }

                items.Add(new ListItem(mediaName, year, rating, overview, poster));
            }

            return items;
        }

        public static ObservableCollection<ListItem> ExtractTvDetailsIntoListItem(SearchContainer<SearchTv> result)
        {
            ObservableCollection<ListItem> items = new ObservableCollection<ListItem>();
            foreach (var item in result.Results)
            {
                string mediaName = item.Name;
                string year = item.FirstAirDate != null ? item.FirstAirDate.Value.Year.ToString() : "";
                string rating = item.VoteAverage.ToString(CultureInfo.CurrentCulture);
                string overview = item.Overview;
                string poster = (item.PosterPath != null) ? SmallPosterBase + item.PosterPath : null;
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
                throw new Exception("NoPoster");
            }

            string folderName = Path.GetFileName(fullFolderPath);
            string localPosterPath = fullFolderPath + @"\" + folderName + ".png";
            string posterUrl = string.Concat(PosterBase, result.PosterPath);
            if (resultType.Equals(MediaTypes.TV))
            {
                SearchTv pickedResult = (SearchTv) result;
                var year = pickedResult.FirstAirDate != null ? pickedResult.FirstAirDate.Value.Year.ToString() : "";
                Util.AddToPickedListDataTable(_listDataTable, localPosterPath, pickedResult.Name,
                    pickedResult.VoteAverage.ToString(CultureInfo.CurrentCulture), fullFolderPath, folderName,
                    year);
            }
            else if (resultType.Equals(MediaTypes.Movie))
            {
                SearchMovie pickedResult = (SearchMovie) result;
                var year = pickedResult.ReleaseDate != null ? pickedResult.ReleaseDate.Value.Year.ToString() : "";
                Util.AddToPickedListDataTable(_listDataTable, localPosterPath, pickedResult.Title,
                    pickedResult.VoteAverage.ToString(CultureInfo.CurrentCulture), fullFolderPath, folderName, year);
            }
            else if (resultType.Equals(MediaTypes.Collection))
            {
                SearchCollection searchResult = (SearchCollection) result;
                Util.AddToPickedListDataTable(_listDataTable, localPosterPath, searchResult.Name, "", fullFolderPath,
                    folderName);
            }
            else if (resultType.Equals(MediaTypes.MTV))
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

            ImageToDownload tempImage = new ImageToDownload()
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
            string mediaType = "";
            if (searchMode == MediaTypes.Movie)
            {
                if (query.ToLower().Contains("collection"))
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
            else if (searchMode == MediaTypes.TV)
            {
                r = _serviceClient.SearchTvShowAsync(query).Result;
                mediaType = MediaTypes.TV;
            }
            else if (searchMode == MediaTypes.MTV)
            {
                r = _serviceClient.SearchMultiAsync(query).Result;
                mediaType = MediaTypes.MTV;
            }

            ResultResponse response = new ResultResponse()
            {
                Result = r,
                MediaType = mediaType
            };
            return response;
        }
    }
}