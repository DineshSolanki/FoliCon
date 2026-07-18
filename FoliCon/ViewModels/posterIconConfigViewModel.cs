namespace FoliCon.ViewModels;

[Localizable(false)]
[SuppressMessage("Performance", "CA1822:Mark members as static",
    Justification = "XAML data binding requires instance properties.")]
[SuppressMessage("Sonar", "S2325:Methods and properties that don't access instance data should be static",
    Justification = "XAML data binding requires instance properties.")]
public class PosterIconConfigViewModel : BindableBase, IDialogAware
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
    private string _iconOverlay;
    public DelegateCommand<object> IconOverlayChangedCommand { get; }

    public PosterIconConfigViewModel()
    {
        Logger.Debug("PosterIconConfigViewModel created");

        // Initialize collections BEFORE tracker restores persisted values
        AvailableOverlays = [];

        Services.Tracker.Configure<PosterIconConfigViewModel>()
            .Property(p => p.IconOverlay, defaultValue: Models.Enums.IconOverlay.Liaher.ToString())
            .PersistOn(nameof(PropertyChanged));
        Services.Tracker.Track(this);
        Logger.Info("Current IconOverlay is {IconOverlay}", IconOverlay);

        // Load available overlays from the provider
        LoadOverlays();

        IconOverlayChangedCommand = new DelegateCommand<object>(delegate(object parameter)
        {
            Logger.Info("Icon overlay changed to {Parameter}", parameter);
            IconOverlay = (string)parameter;
        });
    }

    public string IconOverlay
    {
        get => _iconOverlay;
        set
        {
            if (!SetProperty(ref _iconOverlay, value) || AvailableOverlays == null) return;
            // Update IsActive on overlay items
            foreach (var item in AvailableOverlays)
            {
                item.IsActive = string.Equals(item.OverlayId, value, StringComparison.OrdinalIgnoreCase);
            }
        }
    }

    public ObservableCollection<OverlayItemViewModel> AvailableOverlays { get; }

    public string Title => Lang.SelectPosterIconOverlay;

    private void LoadOverlays()
    {
        try
        {
            var provider = GlobalVariables.OverlayProvider;
            var allOverlays = provider.GetAllOverlays();

            AvailableOverlays.Clear();
            foreach (var overlay in allOverlays)
            {
                var item = new OverlayItemViewModel
                {
                    OverlayId = overlay.Id,
                    DisplayName = overlay.DisplayName,
                    IsBuiltIn = overlay.IsBuiltIn,
                    IsActive = string.Equals(overlay.Id, IconOverlay, StringComparison.OrdinalIgnoreCase),
                    Tags = overlay.Tags
                };
                AvailableOverlays.Add(item);
            }
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to load overlays");
        }
    }

    #region DialogMethods

    public DialogCloseListener RequestClose { get; }

    protected virtual void CloseDialog(string parameter)
    {
        Logger.Info("CloseDialog called with parameter {Parameter}", parameter);
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

/// <summary>
/// ViewModel for an individual overlay item in the config dialog.
/// </summary>
[Localizable(false)]
public class OverlayItemViewModel : BindableBase
{
    public string OverlayId { get; init; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public bool IsBuiltIn { get; set; }
    public string[] Tags { get; set; } = [];

    public bool IsActive
    {
        get;
        set => SetProperty(ref field, value);
    }

    /// <summary>
    /// Returns the demo icon path for built-in overlays.
    /// </summary>
    public string DemoIconPath => OverlayId switch
    {
        "legacy" => "/Resources/mockup_demos/simple/PosterIcon.ico",
        "alternate" => "/Resources/mockup_demos/dvd/PosterIconAlt.ico",
        "liaher" => "/Resources/mockup_demos/liaher/PosterIconLiaher.ico",
        "faelpessoal" => "/Resources/mockup_demos/faelpessoal/PosterIconFaelpessoal.ico",
        "faelpessoal-horizontal" => "/Resources/mockup_demos/faelpessoal/PosterIconFaelpessoalHorizontal.ico",
        "windows11" => "/Resources/mockup_demos/windows11/PosterIconWindows11.ico",
        _ => "/Resources/icons/NoPosterAvailable.png"
    };
}
