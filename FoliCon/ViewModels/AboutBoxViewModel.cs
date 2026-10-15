namespace FoliCon.ViewModels;

[Localizable(false)]
public class AboutBoxViewModel : BindableBase, IDialogAware
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    public static string AppName { get; } = "FoliCon";
    public static string GithubUrl { get; } = "https://github.com/DineshSolanki/FoliCon";
    public static string OverlayStoreUrl { get; } = "https://github.com/DineshSolanki/FoliCon-Overlays";

    public static string VersionTag { get; } = $"v{AssemblyInfo.GetShortVersion()}";

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

    public string Title
    {
        get;
        set => SetProperty(ref field, value);
    } = AssemblyInfo.GetVersionWithoutBuild();

    public string Logo
    {
        get;
        set => SetProperty(ref field, value);
    } = "/Resources/icons/folicon Icon.png";

    public string Description
    {
        get;
        set => SetProperty(ref field, value);
    } = Lang.FoliConDescription;

    public string AdditionalNotes
    {
        get;
        set => SetProperty(ref field, value);
    } = Lang.DevelopedByDinesh;

    public string License
    {
        get;
        set => SetProperty(ref field, value);
    } = Lang.LicenseInfo;

    public string Website
    {
        get;
        set => SetProperty(ref field, value);
    } = "https://aprogrammers.wordpress.com";

    public string Version
    {
        get;
        set => SetProperty(ref field, value);
    } = AssemblyInfo.GetVersion();

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