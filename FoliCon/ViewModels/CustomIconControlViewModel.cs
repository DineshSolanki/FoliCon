// ReSharper disable SwitchStatementMissingSomeEnumCasesNoDefault

namespace FoliCon.ViewModels;

public class CustomIconControlViewModel : BindableBase, IDialogAware, IFileDragDropTarget
{
    private string _selectedDirectory;
    private string _selectedIconsDirectory;
    private ObservableCollection<string> _directories;
    private ObservableCollection<string> _icons;
    private bool _isUndoEnable;
    private bool _keepExactOnly;
    private bool _isBusy;
    private bool _stopSearch;
    public string Title => LangProvider.GetLang("CustomIconSetter");

    private ObservableCollection<string> _undoDirectories = new();
    private ObservableCollection<string> _backupDirectories = new();
    private ObservableCollection<string> _backupIcons = new();
    private string _busyContent = LangProvider.GetLang("CreatingIcons");
    private int _index;
    private int _totalIcons;
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

    public bool IsUndoEnable
    {
        get => _isUndoEnable;
        set => SetProperty(ref _isUndoEnable, value);
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
    public int Index { get => _index; set => SetProperty(ref _index, value); }
    public int TotalIcons { get => _totalIcons; set => SetProperty(ref _totalIcons, value); }
    public bool StopSearch { get => _stopSearch; set => SetProperty(ref _stopSearch, value); }
    #region Declare Delegates

    public DelegateCommand LoadDirectory { get; set; }
    public DelegateCommand LoadIcons { get; set; }
    public DelegateCommand Apply { get; set; }
    public DelegateCommand UndoIcons { get; set; }
    public DelegateCommand<dynamic> KeyPressFolderList { get; set; }
    public DelegateCommand<dynamic> KeyPressIconsList { get; set; }
    public DelegateCommand StopSearchCommand { get; set; }
    public string BusyContent
    {
        get => _busyContent;
        private set => SetProperty(ref _busyContent, value);
    }

    #endregion


    public CustomIconControlViewModel()
    {
        Directories = new ObservableCollection<string>();
        Icons = new ObservableCollection<string>();
        LoadDirectory = new DelegateCommand(LoadDirectoryMethod);
        StopSearchCommand = new DelegateCommand(() => StopSearch = true);
        LoadIcons = new DelegateCommand(LoadIconsMethod);
        Apply = new DelegateCommand(StartProcessing);
        UndoIcons = new DelegateCommand(UndoCreatedIcons);
        KeyPressFolderList = new DelegateCommand<dynamic>(FolderListKeyPress);
        KeyPressIconsList = new DelegateCommand<dynamic>(IconsListKeyPress);
    }

    private void UndoCreatedIcons()
    {
        if (_undoDirectories.Count == 0) return;
        foreach (var folder in _undoDirectories)
        {
            Util.DeleteIconsFromFolder(folder);
        }

        var info = new GrowlInfo
        {
            Message = LangProvider.GetLang("UndoSuccessful"),
            ShowDateTime = false,
            StaysOpen = false
        };
        Growl.SuccessGlobal(info);
        Util.RefreshIconCache();
        SHChangeNotify(SHCNE.SHCNE_ASSOCCHANGED, SHCNF.SHCNF_IDLIST | SHCNF.SHCNF_FLUSHNOWAIT
            , Directory.GetParent(_undoDirectories.First())?.FullName);

        IsUndoEnable = false;
    }

    private void LoadDirectoryMethod()
    {
        var folderBrowserDialog = Util.NewFolderBrowserDialog(LangProvider.GetLang("SelectFolder"));
        var dialogResult = folderBrowserDialog.ShowDialog();
        if (dialogResult != null && (bool)!dialogResult) return;
        _backupDirectories.Clear();
        SelectedDirectory = folderBrowserDialog.SelectedPath;
    }

    private void LoadIconsMethod()
    {
        var folderBrowserDialog = Util.NewFolderBrowserDialog(LangProvider.GetLang("SelectIconsDirectory"));
        var dialogResult = folderBrowserDialog.ShowDialog();
        if (dialogResult != null && (bool)!dialogResult) return;
        _backupIcons.Clear();
        SelectedIconsDirectory = folderBrowserDialog.SelectedPath;
    }

    #region DialogMethods

    public event Action<IDialogResult> RequestClose;

    protected virtual void CloseDialog(string parameter)
    {
        var result = parameter?.ToLower(CultureInfo.InvariantCulture) switch
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
        if (Directories.Count <= 0)
        {
            MessageBox.Show(CustomMessageBox.Error(LangProvider.GetLang("NoFolderOrIconAlready"), LangProvider.GetLang("NoFoldersToProcess")));
            return;
        }

        if (Icons.Count <= 0)
        {
            MessageBox.Show(CustomMessageBox.Error(LangProvider.GetLang("NoIconsSelected"), LangProvider.GetLang("NoIconsToApply")));
            return;
        }

        var iconProcessedCount = MakeIcons();
        var info = new GrowlInfo
        {
            Message = LangProvider.GetLang("IconCreatedWithCount").Format(iconProcessedCount),
            ShowDateTime = false,
            StaysOpen = false
        };
        IsUndoEnable = true;
        _undoDirectories.Clear();
        _undoDirectories = Directories.Select(folder => Path.Combine(SelectedDirectory, folder))
            .ToObservableCollection();
        Growl.SuccessGlobal(info);
        if (MessageBox.Show(
                CustomMessageBox.Ask(
                    $"{LangProvider.GetLang("IconReloadMayTakeTime")} {Environment.NewLine}{LangProvider.GetLang("ToForceReload")} {Environment.NewLine}{LangProvider.GetLang("ConfirmToOpenFolder")}",
                    LangProvider.GetLang("IconCreated"))) == MessageBoxResult.Yes)
            Util.StartProcess(SelectedDirectory + Path.DirectorySeparatorChar);
    }

    private int MakeIcons()
    {
        IsBusy = true;
        Index = 0;
        TotalIcons = Icons.Count;
        StopSearch = false;
        for (var i = 0; i < Directories.Count; ++i)
        {
            if (i >= Icons.Count) break;
            var iconPath = Path.Combine(SelectedIconsDirectory, Icons[i]);
            var folderPath = Path.Combine(SelectedDirectory, Directories[i]);
            var newIconPath = Path.Combine(folderPath, $"{Directories[i]}.ico");
            if (Path.GetExtension(Icons[i].ToLower(CultureInfo.InvariantCulture)) != ".ico")
            {
                var icon = new ProIcon(iconPath).RenderToBitmap();
                iconPath = iconPath.Replace(Path.GetExtension(Icons[i])!, ".ico");
                PngToIcoService.Convert(icon, iconPath);
                icon.Dispose();
            }

            File.Move(iconPath, newIconPath);
            if (!File.Exists(newIconPath)) continue;
            Util.HideFile(newIconPath);
            Util.SetFolderIcon($"{Directories[i]}.ico", folderPath);
            Index++;
            if (StopSearch)
            {
                break;
            }

        }

        Util.ApplyChanges(SelectedDirectory);
        SHChangeNotify(SHCNE.SHCNE_ASSOCCHANGED, SHCNF.SHCNF_IDLIST);
        IsBusy = false;
        return Index;
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

    public void OnFileDrop(string[] filePaths, string senderName)
    {
        switch (senderName)
        {
            case "FolderButton":
            case "FoldersList":
                SelectedDirectory = filePaths.GetValue(0)?.ToString();
                break;
            case "IconButton":
            case "IconsList":
                SelectedIconsDirectory = filePaths.GetValue(0)?.ToString();
                break;
        }
    }
}