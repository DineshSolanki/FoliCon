namespace FoliCon.ViewModels;

public class DialogControlViewModel : BindableBase, IDialogAware
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    public DelegateCommand<string> CloseDialogCommand =>
        field ??= new DelegateCommand<string>(CloseDialog);

    public string Message
    {
        get;
        private set => SetProperty(ref field, value);
    }

    public string Title
    {
        get;
        private set => SetProperty(ref field, value);
    } = Lang.Notification;

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

    public virtual bool CanCloseDialog() => true;

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