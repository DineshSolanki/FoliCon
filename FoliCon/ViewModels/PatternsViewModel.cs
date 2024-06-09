using FoliCon.Models.Data;
using FoliCon.Modules.Configuration;
using FoliCon.Modules.UI;
using FoliCon.Modules.utils;

namespace FoliCon.ViewModels;

public class PatternsViewModel : BindableBase, IDialogAware
{

    #region Variables
    public string Title => "Patterns";

    public string test { get; set; } = "4522";
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
        if (IsValidPattern(trimmedRegex))
        {
            PatternsList.Add(new Pattern(trimmedRegex, true));
        }
    }
    
    private void RemovePattern(Pattern pattern)
    {
        PatternsList.Remove(pattern);
    }
    
    private bool IsValidPattern(string pattern)
    {
        if (!string.IsNullOrWhiteSpace(pattern)
            && !PatternsList.Any(p => p.Regex == pattern)
            && DataUtils.IsValidRegex(pattern)) return true;
        if (!DataUtils.IsValidRegex(pattern))
        {
            MessageBox.Show(CustomMessageBox.Error("The regex you entered is invalid. Please try again.", "Invalid regex"));
        }
        return false;
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
        Services.Settings.Patterns = PatternsList;
        Services.Settings.SubfolderProcessingEnabled = SubfolderProcessingEnabled;
        Services.Settings.Save();
    }

    public virtual void OnDialogOpened(IDialogParameters parameters)
    {
    }

    #endregion DialogMethods
}