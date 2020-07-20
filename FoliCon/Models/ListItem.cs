using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Text;

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
    public string Title { get=> _title;set=> SetProperty(ref _title,value);}
    public string Year { get=> _year;set=> SetProperty(ref _year,value);}
    public string Rating { get=> _rating;set=> SetProperty(ref _rating,value);}
    public string Folder { get=> _folder;set=> SetProperty(ref _folder,value);}
    public string Overview { get=> _overview;set=> SetProperty(ref _overview,value);}
    public string Poster { get=> _poster;set=> SetProperty(ref _poster,value);}
        public ListItem(string _title, string _year, string _rating, string _overview = null, string _poster = null, string _folder = "")
{

        Title = _title;
		Year = _year;
		Rating = _rating;
		Overview = _overview;
		Poster = _poster;
		Folder = _folder;
	}

        public ListItem()
        {
        }
    }
}
