using System;
using System.Collections.ObjectModel;
using System.Linq;
using FoliCon.Modules;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Services.Dialogs;

namespace FoliCon.ViewModels
{
    public class CustomIconControlViewModel : BindableBase, IDialogAware
    {
        private string _selectedDirectory;
        private string _selectedIconsDirectory;
        private ObservableCollection<string> _directories;
        private ObservableCollection<string> _icons;
        public string Title => "Custom icon setter";

        public string SelectedDirectory
        {
            get => _selectedDirectory;
            set
            {
                SetProperty(ref _selectedDirectory, value);
                foreach (var folder in Util.GetFolderNames(value))
                {
                    Directories.Add(folder);
                }
            }
        }

        public string SelectedIconsDirectory
        {
            get => _selectedIconsDirectory;
            set
            {
                SetProperty(ref _selectedIconsDirectory, value);
                foreach (var file in Util.GetFileNamesFromFolder(value).Where(Util.IsPngOrIco))
                {
                    Icons.Add(file);
                }
            }
        }

        public ObservableCollection<string> Directories
        {
            get => _directories;
            set => SetProperty(ref _directories, value);
        }

        public ObservableCollection<string> Icons
        {
            get => _icons;
            set => SetProperty(ref _icons, value);
        }

        #region Declare Delegates

        public DelegateCommand LoadDirectory { get; set; }
        public DelegateCommand LoadIcons { get; set; }
        public DelegateCommand Apply { get; set; }

        #endregion


        public CustomIconControlViewModel()
        {
            Directories = new ObservableCollection<string>();
            Icons = new ObservableCollection<string>();
            LoadDirectory = new DelegateCommand(LoadDirectoryMethod);
            LoadIcons = new DelegateCommand(LoadIconsMethod);
            Apply = new DelegateCommand(MakeIcons);
        }

        private void LoadDirectoryMethod()
        {
            var folderBrowserDialog = Util.NewFolderBrowserDialog("Select Folder");
            var dialogResult = folderBrowserDialog.ShowDialog();
            if (dialogResult != null && (bool) !dialogResult) return;
            SelectedDirectory = folderBrowserDialog.SelectedPath;
        }

        private void LoadIconsMethod()
        {
            var folderBrowserDialog = Util.NewFolderBrowserDialog("Select Icons Directory");
            var dialogResult = folderBrowserDialog.ShowDialog();
            if (dialogResult != null && (bool) !dialogResult) return;
            SelectedIconsDirectory = folderBrowserDialog.SelectedPath;
        }

        #region DialogMethods

        public event Action<IDialogResult> RequestClose;

        protected virtual void CloseDialog(string parameter)
        {
            var result = parameter?.ToLower() switch
            {
                "true" => ButtonResult.OK,
                "false" => ButtonResult.Cancel,
                _ => ButtonResult.None
            };

            RaiseRequestClose(new DialogResult(result));
        }

        public virtual void RaiseRequestClose(IDialogResult dialogResult) =>
            RequestClose?.Invoke(dialogResult);

        public virtual bool CanCloseDialog() => true;

        public virtual void OnDialogClosed()
        {
        }

        public virtual void OnDialogOpened(IDialogParameters parameters)
        {
        }

        #endregion DialogMethods

        private void MakeIcons()
        {
            var found = new ObservableCollection<string>();
            foreach (var folder in Directories)
            {
                var iconName = "";
                if (Icons.Contains($"{folder}.ico")) iconName = $"{folder}.ico";
                else if (Icons.Contains($"{folder}.png")) iconName = $"{folder}.png";

                if (string.IsNullOrEmpty(iconName)) continue;
                // var indexes = new int[2];
                // indexes[0] = Icons.IndexOf(iconName);
                // indexes[1] = Directories.IndexOf(folder);
                // var values = new List<string> {Icons[indexes[0]], Icons[indexes[1]]};
                // Icons.Remove(values[0]);
                // Icons.Remove(values[1]);
                // Icons.Insert(indexes[0], values[1]);
                // Icons.Insert(indexes[1], values[0]);
            }
            
        }
        
    }
}