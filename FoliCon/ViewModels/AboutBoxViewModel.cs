namespace FoliCon.ViewModels;

[Localizable(false)]
public class AboutBoxViewModel : BindableBase, IDialogAware
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
    private string _title = AssemblyInfo.GetVersionWithoutBuild();
    private string _logo = "/Resources/icons/folicon Icon.png";

    private string _description = Lang.FoliConDescription;

    private string _website = "https://aprogrammers.wordpress.com";
    private string _additionalNotes = Lang.DevelopedByDinesh;
    private string _license = Lang.LicenseInfo;
    private string _version = AssemblyInfo.GetVersion();

    public static string AppName { get; } = "FoliCon";
    public static string GithubUrl { get; } = "https://github.com/DineshSolanki/FoliCon";
    public static string OverlayStoreUrl { get; } = "https://github.com/DineshSolanki/FoliCon-Overlays";

    public string VersionTag => $"v{AssemblyInfo.GetShortVersion()}";

    public AboutBoxViewModel()
    {
        WebsiteClickCommand = new DelegateCommand(() => OpenUrl(Website));
        OpenGithubCommand = new DelegateCommand(() => OpenUrl(GithubUrl));
        OpenOverlayStoreCommand = new DelegateCommand(() => OpenUrl(OverlayStoreUrl));
        OpenWebsiteCommand = new DelegateCommand(() => OpenUrl(Website));
        CloseCommand = new DelegateCommand(() => CloseDialog("true"));
        CopyVersionCommand = new DelegateCommand(CopyVersionMethod);
    }

    private static void OpenUrl(string url)
    {
        Logger.Debug("Opening URL {Url}", url);
        ProcessUtils.StartProcess(url);
    }

    private void CopyVersionMethod()
    {
        try
        {
            Clipboard.SetText(Version);
            Logger.Info("Copied version {Version} to clipboard", Version);
        }
        catch (Exception ex)
        {
            Logger.Warn(ex, "Failed to copy version to clipboard");
        }
    }

    public string Title { get => _title; set => SetProperty(ref _title, value); }
    public string Logo { get => _logo; set => SetProperty(ref _logo, value); }
    public string Description { get => _description; set => SetProperty(ref _description, value); }
    public string AdditionalNotes { get => _additionalNotes; set => SetProperty(ref _additionalNotes, value); }
    public string License { get => _license; set => SetProperty(ref _license, value); }
    public string Website { get => _website; set => SetProperty(ref _website, value); }
    public string Version { get => _version; set => SetProperty(ref _version, value); }

    public DelegateCommand WebsiteClickCommand { get; }
    public DelegateCommand OpenGithubCommand { get; }
    public DelegateCommand OpenOverlayStoreCommand { get; }
    public DelegateCommand OpenWebsiteCommand { get; }
    public DelegateCommand CloseCommand { get; }
    public DelegateCommand CopyVersionCommand { get; }

    #region DialogMethods

    public DialogCloseListener RequestClose { get; }

    protected virtual void CloseDialog(string parameter)
    {
        var result = parameter?.ToLower(CultureInfo.InvariantCulture) switch
        {
            "true" => ButtonResult.OK,
            "false" => ButtonResult.Cancel,
            _ => ButtonResult.None
        };

        RequestClose.Invoke(result);
    }

    public virtual bool CanCloseDialog() => true;

    public virtual void OnDialogClosed()
    {
    }

    public virtual void OnDialogOpened(IDialogParameters parameters)
    {
    }

    #endregion DialogMethods
}