#nullable enable
using System;
using System.Threading.Tasks;
using System.Windows.Media;
using FoliCon.Models.Themes;
using FoliCon.Modules.Themes;
using Prism.Commands;
using Prism.Dialogs;
using Prism.Mvvm;
using Color = System.Windows.Media.Color;

namespace FoliCon.ViewModels;

/// <summary>
/// ViewModel for the Universal Deeplink Install & Confirmation Dialog.
/// Enforces user confirmation before installing any theme or overlay package.
/// </summary>
public class DeepLinkInstallDialogViewModel : BindableBase, IDialogAware
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    private readonly IThemeStorageService _themeStorageService;
    private readonly IThemeParser _themeParser;

    public DeepLinkInstallDialogViewModel(
        IThemeStorageService themeStorageService,
        IThemeParser themeParser)
    {
        _themeStorageService = themeStorageService;
        _themeParser = themeParser;

        InstallCommand = new DelegateCommand(async () => await ExecuteInstallAsync(), () => !IsBusy)
            .ObservesProperty(() => IsBusy);
        PreviewCommand = new DelegateCommand(ExecutePreview, () => !IsBusy && IsTheme)
            .ObservesProperty(() => IsBusy);
        CancelCommand = new DelegateCommand(ExecuteCancel);
    }

    #region Properties

    public string Title
    {
        get;
        private set => SetProperty(ref field, value);
    } = "FoliCon Deeplink Request";

    public string TargetType
    {
        get;
        private set => SetProperty(ref field, value);
    } = "Theme";

    public string TargetId
    {
        get;
        private set => SetProperty(ref field, value);
    } = string.Empty;

    public string TargetName
    {
        get;
        private set => SetProperty(ref field, value);
    } = string.Empty;

    public string Repository
    {
        get;
        private set => SetProperty(ref field, value);
    } = "DineshSolanki/FoliCon-Overlays";

    public string StatusMessage
    {
        get;
        private set => SetProperty(ref field, value);
    } = "Review this package before applying.";

    public bool IsTheme => string.Equals(TargetType, "Theme", StringComparison.OrdinalIgnoreCase);

    public bool IsBusy
    {
        get;
        private set => SetProperty(ref field, value);
    }

    public bool IsPreviewOnly
    {
        get;
        private set => SetProperty(ref field, value);
    }

    // Color Swatch Previews for Themes
    public SolidColorBrush SwatchBackground { get; private set; } = new(Color.FromRgb(0x14, 0x14, 0x14));
    public SolidColorBrush SwatchCard { get; private set; } = new(Color.FromRgb(0x1E, 0x1E, 0x1E));
    public SolidColorBrush SwatchPrimary { get; private set; } = new(Color.FromRgb(0x3B, 0x82, 0xF6));
    public SolidColorBrush SwatchAccent { get; private set; } = new(Color.FromRgb(0x60, 0xA5, 0xFA));
    public SolidColorBrush SwatchText { get; private set; } = new(Color.FromRgb(0xF8, 0xFA, 0xFC));

    #endregion

    #region Commands

    public DelegateCommand InstallCommand { get; }
    public DelegateCommand PreviewCommand { get; }
    public DelegateCommand CancelCommand { get; }

    public DialogCloseListener RequestClose { get; }

    #endregion

    public bool CanCloseDialog() => !IsBusy;

    public void OnDialogClosed()
    {
        Logger.Info("DeepLinkInstallDialog closed.");
    }

    public void OnDialogOpened(IDialogParameters parameters)
    {
        TargetType = parameters.GetValue<string>("TargetType") ?? "Theme";
        TargetId = parameters.GetValue<string>("TargetId") ?? string.Empty;
        Repository = parameters.GetValue<string>("Repository") ?? "DineshSolanki/FoliCon-Overlays";
        IsPreviewOnly = parameters.GetValue<bool>("IsPreviewOnly");

        TargetName = TargetId;
        Title = $"Install {TargetType}: {TargetId}";

        if (IsTheme)
        {
            var existingTheme = _themeStorageService.GetThemeById(TargetId);
            if (existingTheme != null)
            {
                TargetName = existingTheme.Name;
                UpdateSwatches(existingTheme);
            }
        }

        Logger.Info("Opened DeepLinkInstallDialog for {Type} '{Id}' from {Repo}", TargetType, TargetId, Repository);
    }

    private void UpdateSwatches(ThemeManifest manifest)
    {
        var dict = _themeParser.SynthesizeResourceDictionary(manifest);
        if (dict["RegionBrush"] is SolidColorBrush bg) SwatchBackground = bg;
        if (dict["SecondaryRegionBrush"] is SolidColorBrush card) SwatchCard = card;
        if (dict["PrimaryBrush"] is SolidColorBrush primary) SwatchPrimary = primary;
        if (dict["AccentBrush"] is SolidColorBrush accent) SwatchAccent = accent;
        if (dict["PrimaryTextBrush"] is SolidColorBrush text) SwatchText = text;

        RaisePropertyChanged(nameof(SwatchBackground));
        RaisePropertyChanged(nameof(SwatchCard));
        RaisePropertyChanged(nameof(SwatchPrimary));
        RaisePropertyChanged(nameof(SwatchAccent));
        RaisePropertyChanged(nameof(SwatchText));
    }

    private async Task ExecuteInstallAsync()
    {
        IsBusy = true;
        StatusMessage = "Applying package...";

        try
        {
            if (IsTheme)
            {
                var manifest = _themeStorageService.GetThemeById(TargetId);
                if (manifest != null)
                {
                    MainWindowViewModel.ApplyThemeManifest(manifest, _themeParser);
                }
            }

            await Task.Delay(200); // Visual pacing
            RequestClose.Invoke(ButtonResult.OK);
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to execute deeplink install for {Id}", TargetId);
            StatusMessage = $"Installation failed: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void ExecutePreview()
    {
        if (IsTheme)
        {
            var manifest = _themeStorageService.GetThemeById(TargetId);
            if (manifest != null)
            {
                MainWindowViewModel.ApplyThemeManifest(manifest, _themeParser);
            }
        }
        RequestClose.Invoke(ButtonResult.OK);
    }

    private void ExecuteCancel()
    {
        RequestClose.Invoke(ButtonResult.Cancel);
    }
}
