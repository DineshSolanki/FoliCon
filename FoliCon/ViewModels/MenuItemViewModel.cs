namespace FoliCon.ViewModels
{
    public class MenuItemViewModel : BindableBase
    {
        public string Header { get; set; }

        public ObservableCollection<MenuItemViewModel> MenuItems { get; set; }
        public string ToolTip { get; set; }
        public DelegateCommand Command { get; set; }

        public MenuItemViewModel(string header, DelegateCommand command, string tooltip = "")
        {
            Header = header;
            ToolTip = tooltip;
            Command = command;
        }

        public MenuItemViewModel()
        {
        }
    }
}