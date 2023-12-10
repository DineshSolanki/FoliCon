using NLog;
using Logger = NLog.Logger;

namespace FoliCon.Modules.utils;

public static class DialogUtils
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
    
    public static void ShowGrowlError(string message)
    {
        Growl.ErrorGlobal(new GrowlInfo { Message = message, ShowDateTime = false });
    }

    public static void ShowGrowlInfo(string message)
    {
        Growl.InfoGlobal(new GrowlInfo { Message = message, ShowDateTime = false, StaysOpen = false });
    }

    public static void ConfirmUpdate(ReleaseInfo ver)
    {
        var message = LangProvider.GetLang("NewVersionFound")
            .Format(ver.TagName, ver.Changelog.Replace("\\n", Environment.NewLine));
        var confirmMessage = LangProvider.GetLang("UpdateNow");
        var cancelMessage = LangProvider.GetLang("Ignore");

        var info = new GrowlInfo
        {
            Message = message,
            ConfirmStr = confirmMessage,
            CancelStr = cancelMessage,
            ShowDateTime = false,
            ActionBeforeClose = isConfirmed =>
            {
                if (!isConfirmed) return true;
                Logger.Debug("Update Confirmed. Starting Update Process");
                ProcessUtils.StartProcess(ver.ReleaseUrl);

                return true;
            }
        };

        Growl.AskGlobal(info);
    }

    public static VistaFolderBrowserDialog NewFolderBrowserDialog(string description)
    {
        Logger.Debug("Creating New Folder Browser Dialog");
        var folderBrowser = new VistaFolderBrowserDialog
        {
            Description = description,
            UseDescriptionForTitle = true
        };
        return folderBrowser;
    }
    
    public static VistaOpenFileDialog NewOpenFileDialog(string description, string filter= "All files (*.*)|*.*")
    {
        Logger.Debug("Creating New Open File Dialog");
        var openFileDialog = new VistaOpenFileDialog
        {
            Filter = filter,
            Multiselect = false,
            CheckFileExists = true,
            CheckPathExists = true,
            DereferenceLinks = true,
            RestoreDirectory = true,
            ShowReadOnly = false,
            ReadOnlyChecked = false,
            Title = description,
            ValidateNames = true,
            InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop)
        };
        return openFileDialog;
    }
}