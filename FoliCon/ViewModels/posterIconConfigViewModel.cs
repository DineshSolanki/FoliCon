namespace FoliCon.ViewModels;

[Localizable(false)]
public class PosterIconConfigViewModel : BindableBase, IDialogAware
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
    private string _iconOverlay;
    public DelegateCommand<object> IconOverlayChangedCommand { get; }

    public PosterIconConfigViewModel()
    {
        Logger.Debug("PosterIconConfigViewModel created");
        Services.Tracker.Configure<PosterIconConfigViewModel>()
            .Property(p => p.IconOverlay, defaultValue: Models.Enums.IconOverlay.Liaher.ToString())
            .PersistOn(nameof(PropertyChanged));
        Services.Tracker.Track(this);
        Logger.Info("Current IconOverlay is {IconOverlay}", IconOverlay);
        IconOverlayChangedCommand = new DelegateCommand<object>(delegate(object parameter)
        {
            Logger.Info("Icon overlay changed to {Parameter}", parameter);
            IconOverlay = (string)parameter;
            
        });
    }

    public string IconOverlay
    {
        get => _iconOverlay;
        set => SetProperty(ref _iconOverlay, value);
    }

    public string Title => Lang.SelectPosterIconOverlay;

    #region DialogMethods

    public event Action<IDialogResult> RequestClose;

    protected virtual void CloseDialog(string parameter)
    {
        Logger.Info("CloseDialog called with parameter {Parameter}", parameter);
        var result = parameter?.ToLower(CultureInfo.InvariantCulture) switch
        {
            "true" => ButtonResult.OK,
            "false" => ButtonResult.Cancel,
            _ => ButtonResult.None
        };

        RaiseRequestClose(new DialogResult(result));
    }

    public virtual void RaiseRequestClose(IDialogResult dialogResult) =>
        RequestClose?.Invoke(dialogResult);

    public virtual bool CanCloseDialog() => true;

    public virtual void OnDialogClosed()
    {
    }

    public virtual void OnDialogOpened(IDialogParameters parameters)
    {
    }

    #endregion DialogMethods
}