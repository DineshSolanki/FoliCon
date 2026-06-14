namespace FoliCon.ViewModels;

public class DialogControlViewModel : BindableBase, IDialogAware
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
    private DelegateCommand<string> _closeDialogCommand;

    public DelegateCommand<string> CloseDialogCommand =>
        _closeDialogCommand ??= new DelegateCommand<string>(CloseDialog);

    private string _message;

    public string Message
    {
        get => _message;
        private set => SetProperty(ref _message, value);
    }

    private string _title = "Notification";

    public string Title
    {
        get => _title;
        private set => SetProperty(ref _title, value);
    }

    public DialogCloseListener RequestClose { get; }

    protected virtual void CloseDialog(string parameter)
    {
        Logger.Info("CloseDialog: {ShouldClose}", parameter);
        var result = parameter?.ToLower(CultureInfo.InvariantCulture) switch
        {
            "true" => ButtonResult.OK,
            "false" => ButtonResult.Cancel,
            _ => ButtonResult.None
        };

        RequestClose.Invoke(result);
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
        Logger.Info("OnDialogOpened: {Parameters}", parameters);
        Message = parameters.GetValue<string>("message");
        Title = parameters.GetValue<string>("title");
    }
}