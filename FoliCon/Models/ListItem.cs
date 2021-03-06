﻿using Prism.Mvvm;

namespace FoliCon.Models
{
    public class ListItem : BindableBase
    {
        private string _title;
        private string _year;
        private string _rating;
        private string _folder;
        private string _overview;
        private string _poster;
        public string Title { get => _title; set => SetProperty(ref _title, value); }
        public string Year { get => _year; set => SetProperty(ref _year, value); }
        public string Rating { get => _rating; set => SetProperty(ref _rating, value); }
        public string Folder { get => _folder; set => SetProperty(ref _folder, value); }
        public string Overview { get => _overview; set => SetProperty(ref _overview, value); }
        public string Poster { get => _poster; set => SetProperty(ref _poster, value); }

        public ListItem(string title, string year, string rating, string overview = null, string poster = null, string folder = "")
        {
            Title = title;
            Year = year;
            Rating = rating;
            Overview = overview;
            Poster = poster;
            Folder = folder;
        }

        public ListItem()
        {
        }
    }
}