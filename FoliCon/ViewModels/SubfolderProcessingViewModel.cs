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

    public string NewPatternText
    {
        get;
        set => SetProperty(ref field, value);
    } = string.Empty;

    #endregion

    #region Commands

    public DelegateCommand<string> AddCommand { get; }
    public DelegateCommand<Pattern> RemoveCommand { get; }
    public DelegateCommand CloseCommand { get; }

    #endregion
    public SubfolderProcessingViewModel()
    {
        AddCommand = new DelegateCommand<string>(AddPattern);
        RemoveCommand = new DelegateCommand<Pattern>(RemovePattern);
        CloseCommand = new DelegateCommand(() => CloseDialog("true"));

        SubfolderProcessingEnabled = Services.Settings.SubfolderProcessingEnabled;
        SubfolderDepthLimit = Services.Settings.SubfolderDepthLimit;
        PatternsList = Services.Settings.Patterns;
    }

    private void AddPattern(string regex)
    {
        var patternToAdd = !string.IsNullOrWhiteSpace(regex) ? regex : NewPatternText;
        var trimmedRegex = patternToAdd?.Trim();
        if (!IsValidPattern(trimmedRegex))
        {
            return;
        }
        Logger.Debug("Adding pattern: {Regex}", trimmedRegex);
        PatternsList.Add(new Pattern(trimmedRegex, true));
        NewPatternText = string.Empty;
    }

    private void RemovePattern(Pattern pattern)
    {
        if (pattern == null || pattern.IsReadOnly)
        {
            return;
        }
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
