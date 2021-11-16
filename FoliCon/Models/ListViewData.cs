using FoliCon.Modules;
using Prism.Mvvm;
using System.Collections.ObjectModel;
using System.Linq;
namespace FoliCon.Models
{
    public class ListViewData : BindableBase
    {
        public Tmdb tmdb { get; set; }
        private ObservableCollection<ListItem> _data;
        private ListItem _selectedItem;
        private int _selectedCount;
        public ObservableCollection<ListItem> Data { get => _data; set => SetProperty(ref _data, value); }
        public ListItem SelectedItem { get => _selectedItem;
            set
            {
                SetProperty(ref _selectedItem, value);
                if(tmdb != null &&  value != null && SelectedItem.TrailerKey != "")
                {
                    var task = tmdb.GetClient().GetMovieVideosAsync(value.Id).Result;
                    if (task.Results.Any())
                    {
                        var i = task.Results.First(i => i.Type == "Trailer");
                        if (i != null)
                        {
                            value.TrailerKey = i.Key;
                            value.Trailer = new System.Uri("https://www.youtube.com/embed/" + i.Key);
                        }
                    }
                }
                    

            }
        }
        public int SelectedCount { get => _selectedCount; set => SetProperty(ref _selectedCount, value); }
    }
}