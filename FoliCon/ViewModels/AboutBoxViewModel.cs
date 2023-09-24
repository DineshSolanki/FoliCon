using FoliCon.Modules.Configuration;
using FoliCon.Modules.utils;
using NLog;
using Logger = NLog.Logger;

namespace FoliCon.ViewModels;

public class AboutBoxViewModel : BindableBase, IDialogAware
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
    private string _title = AssemblyInfo.GetVersionWithoutBuild();
    private string _logo = "/Resources/folicon Icon.png";

    private string _description = LangProvider.GetLang("FoliConDescription");

    private string _website = "https://aprogrammers.wordpress.com";
    private string _additionalNotes = LangProvider.GetLang("DevelopedByDinesh");
    private string _license = LangProvider.GetLang("LicenseInfo");
    private string _version = AssemblyInfo.GetVersion();

    //These properties can also be initialized from Parameters for better re-usability. or From assembly
    public AboutBoxViewModel()
    {
        WebsiteClickCommand = new DelegateCommand(delegate
        {
            Logger.Debug("Opening {Website}",Website);
            ProcessUtils.StartProcess(Website);
        });
    }

    public string Title { get => _title; set => SetProperty(ref _title, value); }
    public string Logo { get => _logo; set => SetProperty(ref _logo, value); }
    public string Description { get => _description; set => SetProperty(ref _description, value); }
    public string AdditionalNotes { get => _additionalNotes; set => SetProperty(ref _additionalNotes, value); }
    public string License { get => _license; set => SetProperty(ref _license, value); }
    public string Website { get => _website; set => SetProperty(ref _website, value); }
    public string Version { get => _version; set => SetProperty(ref _version, value); }
    public DelegateCommand WebsiteClickCommand { get; }

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

    public virtual void RaiseRequestClose(IDialogResult dialogResult)
    {
        RequestClose?.Invoke(dialogResult);
    }

    public virtual bool CanCloseDialog()
    {
        return true;
    }

    public virtual void OnDialogClosed()
    {
    }

    public virtual void OnDialogOpened(IDialogParameters parameters)
    {
    }

    #endregion DialogMethods
}