using FoliCon.Models;
using System;
using System.Collections.Generic;
using System.IO;
using FoliCon.Modules;
using TMDbLib.Objects.Search;
using TMDbLib.Objects.General;
using System.Data;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
namespace FoliCon.Modules
{
    public class TMDB
    {
        private static readonly string smallposterBase = "https://image.tmdb.org/t/p/w200";
        private static readonly string posterBase = "http://image.tmdb.org/t/p/original";
        private readonly DataTable listDataTable;
        private readonly TMDbLib.Client.TMDbClient serviceClient;
        private readonly List<ImageToDownload> imgDownloadList;
        /// <summary>
        /// TMDB Helper Class for Working with IGDB API efficiently for this project.
        /// </summary>
        /// <param name="_serviceClient">Initlized TMDB Client</param>
        /// <param name="_listDataTable">DataTable that stores all the Picked Results.</param>
        /// <param name="_imgDownloadList">List that stores all the images to download.</param>
        public TMDB(ref TMDbLib.Client.TMDbClient _serviceClient,ref DataTable _listDataTable, ref List<ImageToDownload> _imgDownloadList)
        {
            listDataTable=_listDataTable;
            serviceClient=_serviceClient;
            imgDownloadList=_imgDownloadList;
        }

        /// <summary>
        /// Searches TMDB for a query in Specified searh mode
        /// </summary>
        /// <param name="query">Title to search</param>
        /// <param name="searchMode">Search Mode such as Movie,TV</param>
        /// <returns>Returns Search result with its Media Type</returns>
        public async Task<ResultResponse> SearchAsync(string query,string searchMode)
        {
           object r=null;
            string mediaType="";
            if (searchMode == MediaTypes.Movie)
            {
                if (query.ToLower().Contains("collection"))
                {
                    r = serviceClient.SearchCollectionAsync(query).Result;
                    mediaType=MediaTypes.Collection;
                }
                else
                {
                    r=await serviceClient.SearchMovieAsync(query);
                    mediaType=MediaTypes.Movie;
                }
            }
            else if(searchMode== MediaTypes.TV)
            {
                r=serviceClient.SearchTvShowAsync(query).Result;
                mediaType=MediaTypes.TV;
            }
            else if (searchMode==MediaTypes.MTV)
            {
                r=serviceClient.SearchMultiAsync(query).Result;
                mediaType=MediaTypes.MTV;
            }
            ResultResponse response = new ResultResponse()
            {
                Result = r,
                MediaType = mediaType
            };
            return response;
        }
        /// <summary>
        /// Prepares the Selected Result for Download And final List View
        /// </summary>
        /// <param name="ListDataTable"> DataTable that stores all the Picked Results.</param>
        /// <param name="ImgDownloadList">List that stores all the images to download.</param>
        /// <param name="result">Search Response</param>
        /// <param name="resultType">Type of search Response.</param>
        /// <param name="pickIndex">Index of slected result.</param>
        /// <param name="fullFolderPath">Full Path to the current Media Folder</param>
        /// TODO: Merge parameter response and resultType.
        public void ResultPicked(dynamic result, string resultType,string fullFolderPath)
        {
            if (result.PosterPath == null)
            {
                throw new Exception("NoPoster");
            }
            string folderName = Path.GetFileName(fullFolderPath);
            string localPosterPath = fullFolderPath + @"\" + folderName + ".png";
            string posterUrl=string.Concat(posterBase, result.PosterPath);
            if(resultType.Equals(MediaTypes.TV))
            {
                SearchTv searchResult = (SearchTv)result;
                Util.AddToPickedListDataTable(listDataTable, localPosterPath, searchResult.Name,
                    searchResult.VoteAverage.ToString(), fullFolderPath, folderName,
                    searchResult.FirstAirDate.Value.Year.ToString());
            }
            else if (resultType.Equals(MediaTypes.Movie))
            {
                SearchMovie searchResult= (SearchMovie)result;
                Util.AddToPickedListDataTable(listDataTable,localPosterPath,searchResult.Title,
                    searchResult.VoteAverage.ToString(),fullFolderPath,folderName,searchResult.ReleaseDate.Value.Year.ToString());
            }
            else if (resultType.Equals(MediaTypes.Collection))
            {
                SearchCollection searchResult= (SearchCollection)result;
                Util.AddToPickedListDataTable(listDataTable,localPosterPath,searchResult.Name,"",fullFolderPath,folderName);
            }
            else if (resultType.Equals(MediaTypes.MTV))
            {
                
                MediaType mediaType=result.MediaType;
                if (mediaType.Equals(MediaType.Tv))
                {
                    SearchTv pickedResult = result;
                    Util.AddToPickedListDataTable(listDataTable, localPosterPath, pickedResult.Name,
                   pickedResult.VoteAverage.ToString(), fullFolderPath, folderName,
                   pickedResult.FirstAirDate.Value.Year.ToString());
                }
                else if (mediaType.Equals(MediaType.Movie))
                {
                    SearchMovie pickedResult= result;
                    Util.AddToPickedListDataTable(listDataTable, localPosterPath, pickedResult.Title,
                    pickedResult.VoteAverage.ToString(), fullFolderPath, folderName, pickedResult.ReleaseDate.Value.Year.ToString());
                }
            }
            ImageToDownload tempImage = new ImageToDownload()
            {
                LocalPath = localPosterPath,
                RemotePath = new Uri(posterUrl)
            };
            imgDownloadList.Add(tempImage);
        }

    public static ObservableCollection<ListItem> ExtractTvDetailsIntoListItem(SearchContainer<SearchTv> result)
        {
            ObservableCollection<ListItem> items = new ObservableCollection<ListItem>();
            foreach (var item in result.Results)
            {
                    string mediaName = item.Name;
                    string year = item.FirstAirDate != null ? item.FirstAirDate.Value.Year.ToString() : "";
                    string rating = item.VoteAverage.ToString();
                    string overview = item.Overview;
                    string poster = (item.PosterPath != null) ? smallposterBase + item.PosterPath : null;
                    items.Add(new ListItem(mediaName, year, rating, overview, poster));
            }
            return items;
        }
        public static ObservableCollection<ListItem> ExtractMoviesDetailsIntoListItem(SearchContainer<SearchMovie> result)
        {
            ObservableCollection<ListItem> items = new ObservableCollection<ListItem>();
            foreach (var item in result.Results)
            {
                string mediaName = item.Title;
                string year = item.ReleaseDate != null ? item.ReleaseDate.Value.Year.ToString() : "";
                string rating = item.VoteAverage.ToString();
                string overview = item.Overview;
                string poster = (item.PosterPath != null) ? smallposterBase + item.PosterPath : null;
                items.Add(new ListItem(mediaName, year, rating, overview, poster));
            }
            return items;
        }
        public static ObservableCollection<ListItem> ExtractResourceDetailsIntoListItem(SearchContainer<SearchBase> result)
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
                if (mediaType.Equals(MediaType.Tv))
                {
                    SearchTv res =(SearchTv) item;
                    mediaName = res.Name;
                    year = (res.FirstAirDate != null) ? res.FirstAirDate.Value.Year.ToString() : "";
                    rating = res.VoteAverage.ToString();
                    overview = res.Overview;
                    poster = (res.PosterPath != null) ? smallposterBase + res.PosterPath : null;
                }
                else if (mediaType.Equals(MediaType.Movie))
                {
                    SearchMovie res =(SearchMovie) item;
                    mediaName = res.Title;
                    year = (res.ReleaseDate != null) ? res.ReleaseDate.Value.Year.ToString() : "";
                    rating = res.VoteAverage.ToString();
                    overview = res.Overview;
                    poster = (res.PosterPath != null) ? smallposterBase + res.PosterPath : null;
                }

                items.Add(new ListItem(mediaName, year, rating, overview, poster));
            }
            return items;
        }
        public static ObservableCollection<ListItem> ExtractCollectionDetailsIntoListItem(SearchContainer<SearchCollection> result)
        {
            ObservableCollection<ListItem> items = new ObservableCollection<ListItem>();
            foreach (var item in result.Results)
            {
                string mediaName = item.Name;
                string year = "";
                string rating = "";
                string overview = "";
                string poster = Convert.ToString((item.PosterPath != null) ? smallposterBase + item.PosterPath : null);
                items.Add(new ListItem(mediaName, year, rating, overview, poster));
            }
            return items;
        }
    }

}
