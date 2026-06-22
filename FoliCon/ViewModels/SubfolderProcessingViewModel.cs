namespace FoliCon.ViewModels;

[SuppressMessage("Performance", "CA1822:Mark members as static",
    Justification = "XAML data binding requires instance properties.")]
[SuppressMessage("Sonar", "S2325:Methods and properties that don't access instance data should be static",
    Justification = "XAML data binding requires instance properties.")]
public class SubfolderProcessingViewModel : BindableBase, IDialogAware
{

    #region Variables
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    public string Title => Lang.SubfolderProcessing;

    public ObservableCollection<Pattern> PatternsList
    {
        get;
        set => SetProperty(ref field, value);
    }

    public bool SubfolderProcessingEnabled
    {
        get;
        set => SetProperty(ref field, value);
    }

    public int SubfolderDepthLimit
    {
        get;
        set => SetProperty(ref field, value);
    }

    #endregion

    #region Commands

    public DelegateCommand<string> AddCommand { get; }
    public DelegateCommand<Pattern> RemoveCommand { get; }

    #endregion
    public SubfolderProcessingViewModel()
    {
        AddCommand = new DelegateCommand<string>(AddPattern);
        RemoveCommand = new DelegateCommand<Pattern>(RemovePattern);

        SubfolderProcessingEnabled = Services.Settings.SubfolderProcessingEnabled;
        SubfolderDepthLimit = Services.Settings.SubfolderDepthLimit;
        PatternsList = Services.Settings.Patterns;
    }

    private void AddPattern(string regex)
    {
        var trimmedRegex = regex?.Trim();
        if (!IsValidPattern(trimmedRegex))
        {
            return;
        }
        Logger.Debug("Adding pattern: {Regex}", trimmedRegex);
        PatternsList.Add(new Pattern(trimmedRegex, true));
    }

    private void RemovePattern(Pattern pattern)
    {
        Logger.Debug("Removing pattern: {Regex}", pattern.Regex);
        PatternsList.Remove(pattern);
    }

    private bool IsValidPattern(string pattern)
    {
        if (!string.IsNullOrWhiteSpace(pattern)
            && PatternsList.All(p => p.Regex != pattern) && DataUtils.IsValidRegex(pattern))
        {
            return true;
        }
        if (!DataUtils.IsValidRegex(pattern))
        {
            MessageBox.Show(CustomMessageBox.Error(Lang.InvalidRegexMessage,
                Lang.InvalidRegex));
        }
        return false;
    }

    #region DialogMethods

    public DialogCloseListener RequestClose { get; }

    public virtual bool CanCloseDialog() => true;

    public virtual void OnDialogClosed()
    {
        Services.Settings.Patterns = PatternsList;
        Services.Settings.SubfolderProcessingEnabled = SubfolderProcessingEnabled;
        Services.Settings.SubfolderDepthLimit = SubfolderDepthLimit;
        Services.Settings.Save();
    }

    public virtual void OnDialogOpened(IDialogParameters parameters)
    {
    }

    #endregion DialogMethods
}
