using Prism.Mvvm;
using System.Collections.ObjectModel;

namespace FoliCon.Models
{
    public class ListViewData : BindableBase
    {
        private ObservableCollection<ListItem> _data;
        private ListItem _selectedItem;
        private int _selectedCount;
        public ObservableCollection<ListItem> Data { get => _data; set => SetProperty(ref _data, value); }
        public ListItem SelectedItem { get => _selectedItem; set => SetProperty(ref _selectedItem, value); }
        public int SelectedCount { get => _selectedCount; set => SetProperty(ref _selectedCount, value); }
    }
}