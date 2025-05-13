// ReSharper disable SwitchStatementMissingSomeEnumCasesNoDefault

using FoliCon.Modules.Media;

namespace FoliCon.ViewModels;

[Localizable(false)]
public class CustomIconControlViewModel : BindableBase, IDialogAware, IFileDragDropTarget
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
    
    public IDropTarget ReOrderDropHandler { get; } = new ReOrderDropHandler();
    private string _selectedDirectory;
    private string _selectedIconsDirectory;
    private ObservableCollection<string> _directories;
    private ObservableCollection<string> _icons;
    private bool _isUndoEnable;
    private bool _keepExactOnly;
    private bool _isBusy;
    private bool _stopSearch;
    private string _selectedIconOverlay = "Liaher";
    private bool _isOverlayEnabled = true;
    private bool _isRatingEnabled = true;
    public string Title => Lang.CustomIconSetter;

    private ObservableCollection<string> _undoDirectories = [];
    private ObservableCollection<string> _backupDirectories = [];
    private ObservableCollection<string> _backupIcons = [];
    private string _busyContent = Lang.CreatingIcons;
    private int _index;
    private int _totalIcons;
    public bool KeepExactOnly
    {
        get => _keepExactOnly;
        set
        {
            SetProperty(ref _keepExactOnly, value);
            if (value)
            {
                RemoveNotMatching();
            }
            else
            {
                RestoreCollections();
            }
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

    public string SelectedIconOverlay
    {
        get => _selectedIconOverlay;
        set => SetProperty(ref _selectedIconOverlay, value);
    }

    public bool IsOverlayEnabled
    {
        get => _isOverlayEnabled;
        set => SetProperty(ref _isOverlayEnabled, value);
    }

    public bool IsRatingEnabled
    {
        get => _isRatingEnabled;
        set => SetProperty(ref _isRatingEnabled, value);
    }

    private string SelectedDirectory
    {
        get => _selectedDirectory;
        set
        {
            SetProperty(ref _selectedDirectory, value);
            Directories.Clear();
            KeepExactOnly = false;
            foreach (var folder in FileUtils.GetFolderNames(value))
            {
                Directories.Add(folder);
            }
        }
    }

    private string SelectedIconsDirectory
    {
        get => _selectedIconsDirectory;
        set
        {
            SetProperty(ref _selectedIconsDirectory, value);
            Logger.Debug("Selecting PNG and ICO files from {Value}", value);
            KeepExactOnly = false;
            Icons.Clear();
            foreach (var file in FileUtils.GetFileNamesFromFolder(value).Where(FileUtils.IsPngOrIco))
            {
                Icons.Add(file);
            }
        }
    }

    public ObservableCollection<string> Directories
    {
        get => _directories;
        private set => SetProperty(ref _directories, value);
    }

    public ObservableCollection<string> Icons
    {
        get => _icons;
        private set => SetProperty(ref _icons, value);
    }
    public int Index { get => _index; set => SetProperty(ref _index, value); }
    public int TotalIcons { get => _totalIcons; set => SetProperty(ref _totalIcons, value); }
    private bool StopSearch { get => _stopSearch; set => SetProperty(ref _stopSearch, value); }
    #region Declare Delegates

    public DelegateCommand LoadDirectory { get; set; }
    public DelegateCommand LoadIcons { get; set; }
    public DelegateCommand Apply { get; set; }
    public DelegateCommand UndoIcons { get; set; }
    public DelegateCommand<dynamic> KeyPressFolderList { get; set; }
    public DelegateCommand<dynamic> KeyPressIconsList { get; set; }
    public DelegateCommand StopSearchCommand { get; set; }
    public DelegateCommand<object> IconOverlayChangedCommand { get; }
    public string BusyContent
    {
        get => _busyContent;
        private set => SetProperty(ref _busyContent, value);
    }

    #endregion


    public CustomIconControlViewModel()
    {
        Directories = [];
        Icons = [];
        LoadDirectory = new DelegateCommand(LoadDirectoryMethod);
        StopSearchCommand = new DelegateCommand(() => StopSearch = true);
        LoadIcons = new DelegateCommand(LoadIconsMethod);
        Apply = new DelegateCommand(StartProcessing);
        UndoIcons = new DelegateCommand(UndoCreatedIcons);
        KeyPressFolderList = new DelegateCommand<dynamic>(FolderListKeyPress);
        KeyPressIconsList = new DelegateCommand<dynamic>(IconsListKeyPress);
        IconOverlayChangedCommand = new DelegateCommand<object>(parameter =>
        {
            if (parameter is string overlay)
            {
                SelectedIconOverlay = overlay;
            }
        });
    }

    private async void UndoCreatedIcons()
    {
        Logger.Debug("Deleting created icons from {Count} folders", _undoDirectories.Count);
        if (_undoDirectories.Count == 0)
        {
            return;
        }

        foreach (var folder in _undoDirectories)
        {
            FileUtils.DeleteIconsFromFolder(folder);
        }

        var info = new GrowlInfo
        {
            Message = Lang.UndoSuccessful,
            ShowDateTime = false,
            StaysOpen = false
        };
        Growl.SuccessGlobal(info);
        await ProcessUtils.RefreshIconCacheAsync();
        SHChangeNotify(SHCNE.SHCNE_ASSOCCHANGED, SHCNF.SHCNF_IDLIST | SHCNF.SHCNF_FLUSHNOWAIT
            , Directory.GetParent(_undoDirectories[0])?.FullName);

        IsUndoEnable = false;
    }

    private void LoadDirectoryMethod()
    {
        var folderBrowserDialog = DialogUtils.NewFolderBrowserDialog(Lang.SelectFolder);
        var dialogResult = folderBrowserDialog.ShowDialog();
        if (dialogResult != null && (bool)!dialogResult)
        {
            return;
        }

        _backupDirectories.Clear();
        SelectedDirectory = folderBrowserDialog.SelectedPath;
        Logger.Debug("Selected directory: {SelectedDirectory}", SelectedDirectory);
    }

    private void LoadIconsMethod()
    {
        var folderBrowserDialog = DialogUtils.NewFolderBrowserDialog(Lang.SelectIconsDirectory);
        var dialogResult = folderBrowserDialog.ShowDialog();
        if (dialogResult != null && (bool)!dialogResult)
        {
            return;
        }

        _backupIcons.Clear();
        SelectedIconsDirectory = folderBrowserDialog.SelectedPath;
        Logger.Debug("Selected icons directory: {SelectedIconsDirectory}", SelectedIconsDirectory);
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

    protected virtual void RaiseRequestClose(IDialogResult dialogResult) =>
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
        Logger.Debug("Directories count: {Count}, Icons count: {IconCount}", Directories.Count, Icons.Count);
        if (Directories.Count <= 0 || SelectedDirectory is null)
        {
            Logger.Warn("No folders to process");
            MessageBox.Show(CustomMessageBox.Error(Lang.NoFolderOrIconAlready, Lang.NoFoldersToProcess));
            return;
        }

        if (Icons.Count <= 0 || SelectedIconsDirectory is null)
        {
            MessageBox.Show(CustomMessageBox.Error(Lang.NoIconsSelected, Lang.NoIconsToApply));
            return;
        }

        var iconProcessedCount = MakeIcons();
        var info = new GrowlInfo
        {
            Message = Lang.IconCreatedWithCount.Format(iconProcessedCount),
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
                    $"{Lang.IconReloadMayTakeTime} {Environment.NewLine}{Lang.ToForceReload} {Environment.NewLine}{Lang.ConfirmToOpenFolder}",
                    Lang.IconCreated)) == MessageBoxResult.Yes)
        {
            ProcessUtils.StartProcess(SelectedDirectory + Path.DirectorySeparatorChar);
        }
    }

    private int MakeIcons()
    {
        IsBusy = true;
        Index = 0;
        TotalIcons = Icons.Count;
        StopSearch = false;
        var iconOverlay = SelectedIconOverlay switch
        {
            "Legacy" => Models.Enums.IconOverlay.Legacy,
            "Alternate" => Models.Enums.IconOverlay.Alternate,
            "Liaher" => Models.Enums.IconOverlay.Liaher,
            "Faelpessoal" => Models.Enums.IconOverlay.Faelpessoal,
            "FaelpessoalHorizontal" => Models.Enums.IconOverlay.FaelpessoalHorizontal,
            _ => Models.Enums.IconOverlay.Liaher
        };
        for (var i = 0; i < Directories.Count; ++i)
        {
            if (i >= Icons.Count)
            {
                break;
            }

            var iconPath = Path.Combine(SelectedIconsDirectory, Icons[i]);
            var folderPath = Path.Combine(SelectedDirectory, Directories[i]);
            var newIconPath = Path.Combine(folderPath, $"{Directories[i]}.ico");

            Logger.Debug("Creating icon for {Folder} from {Icon}, new Path is: {NewIconPath}",
                folderPath, iconPath, newIconPath);

            // Use PNG as input if not already ICO
            if (Path.GetExtension(Icons[i].ToLower(CultureInfo.InvariantCulture)) != ".ico")
            {
                Logger.Info("Converting {Icon} to .ico", iconPath);
                Bitmap icon = null;
                // Use overlay logic similar to IconUtils.BuildFolderIco
                var rating = IsRatingEnabled ? "8.0" : string.Empty; // Placeholder, can be improved
                var ratingVisibility = IsRatingEnabled ? "visible" : "hidden";
                var mockupVisibility = IsOverlayEnabled ? "visible" : "hidden";
                var mediaTitle = string.Empty;
                switch (iconOverlay)
                {
                    case Models.Enums.IconOverlay.Legacy:
                        icon = new Views.PosterIcon(new Models.Data.PosterIcon(iconPath, rating, ratingVisibility, mockupVisibility)).RenderToBitmap();
                        break;
                    case Models.Enums.IconOverlay.Alternate:
                        icon = new Views.PosterIconAlt(new Models.Data.PosterIcon(iconPath, rating, ratingVisibility, mockupVisibility)).RenderToBitmap();
                        break;
                    case Models.Enums.IconOverlay.Liaher:
                        icon = new Views.PosterIconLiaher(new Models.Data.PosterIcon(iconPath, rating, ratingVisibility, mockupVisibility)).RenderToBitmap();
                        break;
                    case Models.Enums.IconOverlay.Faelpessoal:
                        icon = new Views.PosterIconFaelpessoal(new Models.Data.PosterIcon(iconPath, rating, ratingVisibility, mockupVisibility, mediaTitle)).RenderToBitmap();
                        break;
                    case Models.Enums.IconOverlay.FaelpessoalHorizontal:
                        icon = new Views.PosterIconFaelpessoalHorizontal(new Models.Data.PosterIcon(iconPath, rating, ratingVisibility, mockupVisibility, mediaTitle)).RenderToBitmap();
                        break;
                    default:
                        icon = new Views.PosterIcon(new Models.Data.PosterIcon(iconPath, rating, ratingVisibility, mockupVisibility)).RenderToBitmap();
                        break;
                }
                iconPath = iconPath.Replace(Path.GetExtension(Icons[i])!, ".ico");
                PngToIcoService.Convert(icon, iconPath);
                icon.Dispose();
                Logger.Info("Converted {Icon} to .ico", iconPath);
            }
            Logger.Info("Moving {Icon} to {NewIconPath}", iconPath, newIconPath);
            File.Move(iconPath, newIconPath);
            if (!FileUtils.FileExists(newIconPath))
            {
                continue;
            }

            FileUtils.HideFile(newIconPath);
            FileUtils.SetFolderIcon($"{Directories[i]}.ico", folderPath);
            Index++;
            if (!StopSearch)
            {
                continue;
            }

            Logger.Warn("User stopped search");
            break;
        }

        FileUtils.ApplyChanges(SelectedDirectory);
        SHChangeNotify(SHCNE.SHCNE_ASSOCCHANGED, SHCNF.SHCNF_IDLIST);
        IsBusy = false;
        return Index;
    }

    private void RemoveNotMatching()
    {
        Logger.Debug("Removing unmatching icons");
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
        Logger.Debug("Restoring collections from backup icon count: {Count}, backup directory count: {DirectoryCount}", 
            _backupIcons.Count, _backupDirectories.Count);
        
        if (_backupIcons.Count == 0 || _backupDirectories.Count == 0)
        {
            return;
        }

        Icons.Clear();
        Directories.Clear();
        Icons = _backupIcons.ToObservableCollection();
        Directories = _backupDirectories.ToObservableCollection();
        _backupIcons.Clear();
        _backupDirectories.Clear();
    }

    private void FolderListKeyPress(dynamic selectedItems)
    {
        Logger.Debug("Removing {Count} items from folder list", selectedItems.Count);
        var temp = new List<object>(selectedItems);
        foreach (string i in temp)
        {
            Directories.Remove(i);
        }

        KeepExactOnly = false;
        Logger.Debug("Removed {@SelectedItems} from folder list", selectedItems);
    }

    private void IconsListKeyPress(dynamic selectedItems)
    {
        Logger.Debug("Removing {Count} items from icons list", selectedItems.Count);
        var temp = new List<object>(selectedItems);
        foreach (string i in temp)
        {
            Icons.Remove(i);
        }

        KeepExactOnly = false;
        Logger.Debug("Removed {@SelectedItems} from icons list", selectedItems);
    }

    public void OnFileDrop(string[] filePaths, string senderName)
    {
        Logger.Debug("File dropped on {SenderName}", senderName);
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