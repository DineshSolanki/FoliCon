using FoliCon.Models.Data;
using FoliCon.Modules.Configuration;
using FoliCon.Modules.UI;
using FoliCon.Modules.utils;
using NLog;

namespace FoliCon.ViewModels;

public class PatternsViewModel : BindableBase, IDialogAware
{

    #region Variables
    private static readonly NLog.Logger Logger = LogManager.GetCurrentClassLogger();

    public string Title => "Patterns";

    private ObservableCollection<Pattern> _patterns;
    private bool _subfolderProcessingEnabled;
        
    public ObservableCollection<Pattern> PatternsList
    {
        get => _patterns;
        set => SetProperty(ref _patterns, value);
    }
        
    public bool SubfolderProcessingEnabled
    {
        get => _subfolderProcessingEnabled;
        set => SetProperty(ref _subfolderProcessingEnabled, value);
    }
        
    #endregion

    #region Commands

    public DelegateCommand<string> AddCommand { get; }
    public DelegateCommand<Pattern> RemoveCommand { get; }
    
    #endregion
    public PatternsViewModel()
    {
        AddCommand = new DelegateCommand<string>(AddPattern);
        RemoveCommand = new DelegateCommand<Pattern>(RemovePattern);
            
        SubfolderProcessingEnabled = Services.Settings.SubfolderProcessingEnabled;
        PatternsList = Services.Settings.Patterns;
    }
        
    private void AddPattern(string regex)
    {
        var trimmedRegex = regex?.Trim();
        if (!IsValidPattern(trimmedRegex)) return;
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
            && PatternsList.All(p => p.Regex != pattern) && DataUtils.IsValidRegex(pattern)) return true;
        if (!DataUtils.IsValidRegex(pattern))
        {
            MessageBox.Show(CustomMessageBox.Error("The regex pattern entered is invalid. Ensure it follows the correct syntax (e.g., ^[A-Za-z0-9]+$).", "Invalid regex"));
        }
        return false;
    }
    
    #region DialogMethods

    public event Action<IDialogResult> RequestClose;

    public virtual bool CanCloseDialog()
    {
        return true;
    }

    public virtual void OnDialogClosed()
    {
        Services.Settings.Patterns = PatternsList;
        Services.Settings.SubfolderProcessingEnabled = SubfolderProcessingEnabled;
        Services.Settings.Save();
    }

    public virtual void OnDialogOpened(IDialogParameters parameters)
    {
    }

    #endregion DialogMethods
}