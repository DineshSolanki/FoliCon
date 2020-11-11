using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.IO;
using System.Linq;
using FoliCon.Modules;
using HandyControl.Controls;
using HandyControl.Data;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Services.Dialogs;
using static Vanara.PInvoke.Shell32;

namespace FoliCon.ViewModels
{
    public class CustomIconControlViewModel : BindableBase, IDialogAware
    {
        private string _selectedDirectory;
        private string _selectedIconsDirectory;
        private ObservableCollection<string> _directories;
        private ObservableCollection<string> _icons;
        private bool _keepExactOnly;
        private bool _isBusy;
        public string Title => "Custom icon setter";

        private ObservableCollection<string> _backupDirectories = new ObservableCollection<string>();
        private ObservableCollection<string> _backupIcons = new ObservableCollection<string>();
        private string _busyContent;

        public bool KeepExactOnly
        {
            get => _keepExactOnly;
            set
            {
                SetProperty(ref _keepExactOnly, value);
                if (value) RemoveNotMatching();
                else RestoreCollections();
            }
        }
        public bool IsBusy
        {
            get => _isBusy;
            set => SetProperty(ref _isBusy, value);
        }
        public string SelectedDirectory
        {
            get => _selectedDirectory;
            set
            {
                SetProperty(ref _selectedDirectory, value);
                Directories.Clear();
                KeepExactOnly = false;
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
                KeepExactOnly = false;
                Icons.Clear();
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
        public DelegateCommand<dynamic> KeyPressFolderList { get; set; }
        public DelegateCommand<dynamic> KeyPressIconsList { get; set; }
        public string BusyContent { get => _busyContent; private set => SetProperty(ref _busyContent,value); }

        #endregion


        public CustomIconControlViewModel()
        {
            Directories = new ObservableCollection<string>();
            Icons = new ObservableCollection<string>();
            LoadDirectory = new DelegateCommand(LoadDirectoryMethod);
            LoadIcons = new DelegateCommand(LoadIconsMethod);
            Apply = new DelegateCommand(StartProcessing);
            KeyPressFolderList = new DelegateCommand<dynamic>(FolderListKeyPress);
            KeyPressIconsList = new DelegateCommand<dynamic>(IconsListKeyPress);
        }

        private void LoadDirectoryMethod()
        {
            var folderBrowserDialog = Util.NewFolderBrowserDialog("Select Folder");
            var dialogResult = folderBrowserDialog.ShowDialog();
            if (dialogResult != null && (bool)!dialogResult) return;
            _backupDirectories.Clear();
            SelectedDirectory = folderBrowserDialog.SelectedPath;
        }

        private void LoadIconsMethod()
        {
            var folderBrowserDialog = Util.NewFolderBrowserDialog("Select Icons Directory");
            var dialogResult = folderBrowserDialog.ShowDialog();
            if (dialogResult != null && (bool)!dialogResult) return;
            _backupIcons.Clear();
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
        private void StartProcessing()
        {
            var iconProcessedCount = MakeIcons();
            var info = new GrowlInfo
            {
                Message = $"{iconProcessedCount} Icon created",
                ShowDateTime = false,
                StaysOpen = false
            };
            Growl.SuccessGlobal(info);
            switch (MessageBox.Ask("Note:The Icon may take some time to reload. " + Environment.NewLine +
                                   " To Force Reload, click on Restart Explorer " + Environment.NewLine +
                                   "Click \"Confirm\" to open folder.", "Icon(s) Created"))
            {
                case System.Windows.MessageBoxResult.OK:
                    Util.StartProcess(SelectedDirectory + Path.DirectorySeparatorChar);
                    break;
            }
        }
        private int MakeIcons()
        {
            IsBusy = true;
            var count = 0;
            for (var i = 0; i < Directories.Count; ++i)
            {
                var iconPath = Path.Combine(SelectedIconsDirectory, Icons[i]);
                var folderPath = Path.Combine(SelectedDirectory, Directories[i]);
                var newIconPath = Path.Combine(folderPath, Directories[i] + ".ico");
                if (i >= Icons.Count) break;
                if (!Path.GetExtension(Icons[i]).ToLower().Equals(".ico"))
                {
                    var icon = new ProIcon(iconPath).RenderToBitmap();
                    PngToIcoService.Convert(icon, iconPath.Replace(Path.GetExtension(Icons[i]), ".ico"));
                    icon.Dispose();
                }
                File.Copy(iconPath, newIconPath);
                if (File.Exists(newIconPath))
                {
                    Util.HideIcons(newIconPath);
                    Util.SetFolderIcon(newIconPath, folderPath);
                    count++;
                }
                BusyContent = $"Creating icon {count}";
            }
            Util.ApplyChanges(SelectedDirectory);
            SHChangeNotify(SHCNE.SHCNE_ASSOCCHANGED, SHCNF.SHCNF_IDLIST);
            IsBusy = false;
            return count;
        }
        private void RemoveNotMatching()
        {
            _backupDirectories.Clear();
            _backupIcons.Clear();
            _backupIcons = Icons;
            _backupDirectories = Directories;
            Icons = Icons.OrderBySequence(Directories, name => name.WithoutExt())
                .ToObservableCollection();
            var temp = new ObservableCollection<string>();
            Icons.ToList().ForEach(i => temp.Add(i.WithoutExt()));
            Directories = Directories.OrderBySequence(temp, name => name)
                .ToObservableCollection();
        }
        private void RestoreCollections()
        {
            if (_backupIcons.Count == 0 || _backupDirectories.Count == 0) return;
            Icons.Clear();
            Directories.Clear();
            Icons = _backupIcons.ToList().ToObservableCollection();
            Directories = _backupDirectories.ToList().ToObservableCollection();
            _backupIcons.Clear();
            _backupDirectories.Clear();
        }
        private void FolderListKeyPress(dynamic selectedItems)
        {
            var temp = new List<object>(selectedItems);
            foreach (string i in temp)
            {
                Directories.Remove(i);
            }
            KeepExactOnly = false;
        }
        private void IconsListKeyPress(dynamic selectedItems)
        {
            var temp = new List<object>(selectedItems);
            foreach (string i in temp)
            {
                Icons.Remove(i);
            }
            KeepExactOnly = false;
        }
    }
}