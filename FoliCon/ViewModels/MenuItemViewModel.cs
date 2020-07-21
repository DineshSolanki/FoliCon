using Prism.Commands;
using Prism.Mvvm;
using System.Collections.ObjectModel;
using System.Windows;

namespace FoliCon.ViewModels
{
    public class MenuItemViewModel : BindableBase
    {
        public string Header { get; set; }

        public ObservableCollection<MenuItemViewModel> MenuItems { get; set; }
        public string ToolTip { get; set; }
        public DelegateCommand Command { get; set; }

        private void Execute()
        {
            MessageBox.Show("Clicked at " + Header);
        }

        public MenuItemViewModel(string header, DelegateCommand command, string tooltip = "")
        {
            Header = header;
            ToolTip = tooltip;
            Command = command;
            //Command = new DelegateCommand(Execute);
        }

        public MenuItemViewModel()
        {
        }
    }
}