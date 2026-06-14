using FoliCon.Modules.Validation;

namespace FoliCon.ViewModels;

public class OnboardingWizardViewModel : BindableBase, IDialogAware
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    #region Step Management

    private int _currentStep;
    public int CurrentStep
    {
        get => _currentStep;
        set
        {
            if (SetProperty(ref _currentStep, value))
            {
                RaisePropertyChanged(nameof(IsWelcomeStep));
                RaisePropertyChanged(nameof(IsTmdbStep));
                RaisePropertyChanged(nameof(IsIgdbStep));
                RaisePropertyChanged(nameof(IsDeviantArtStep));
                RaisePropertyChanged(nameof(IsSummaryStep));
                RaisePropertyChanged(nameof(CanGoBack));
                RaisePropertyChanged(nameof(CanGoNext));
                RaisePropertyChanged(nameof(NextButtonText));
            }
        }
    }

    public bool IsWelcomeStep => CurrentStep == 0;
    public bool IsTmdbStep => CurrentStep == 1;
    public bool IsIgdbStep => CurrentStep == 2;
    public bool IsDeviantArtStep => CurrentStep == 3;
    public bool IsSummaryStep => CurrentStep == 4;
    public bool CanGoBack => CurrentStep > 0 && CurrentStep < 4;
    public bool CanGoNext => CurrentStep is >= 1 and < 4;
    public string NextButtonText => CurrentStep == 3 ? Lang.OnboardingFinish : Lang.OnboardingNext;

    #endregion

    #region TMDB Properties

    private string _tmdbKey = "";
    public string TmdbKey
    {
        get => _tmdbKey;
        set
        {
            if (SetProperty(ref _tmdbKey, value))
                ResetTmdbValidation();
        }
    }

    private bool _isTmdbValidating;
    public bool IsTmdbValidating { get => _isTmdbValidating; set => SetProperty(ref _isTmdbValidating, value); }

    private bool? _isTmdbValid;
    public bool? IsTmdbValid { get => _isTmdbValid; set => SetProperty(ref _isTmdbValid, value); }

    private string _tmdbValidationMessage = "";
    public string TmdbValidationMessage { get => _tmdbValidationMessage; set => SetProperty(ref _tmdbValidationMessage, value); }

    public bool IsTmdbConfigured => !string.IsNullOrWhiteSpace(TmdbKey);

    #endregion

    #region IGDB Properties

    private string _igdbClientId = "";
    public string IgdbClientId
    {
        get => _igdbClientId;
        set
        {
            if (SetProperty(ref _igdbClientId, value))
                ResetIgdbValidation();
        }
    }

    private string _igdbClientSecret = "";
    public string IgdbClientSecret
    {
        get => _igdbClientSecret;
        set
        {
            if (SetProperty(ref _igdbClientSecret, value))
                ResetIgdbValidation();
        }
    }

    private bool _isIgdbValidating;
    public bool IsIgdbValidating { get => _isIgdbValidating; set => SetProperty(ref _isIgdbValidating, value); }

    private bool? _isIgdbValid;
    public bool? IsIgdbValid { get => _isIgdbValid; set => SetProperty(ref _isIgdbValid, value); }

    private string _igdbValidationMessage = "";
    public string IgdbValidationMessage { get => _igdbValidationMessage; set => SetProperty(ref _igdbValidationMessage, value); }

    public bool IsIgdbConfigured => !string.IsNullOrWhiteSpace(IgdbClientId) && !string.IsNullOrWhiteSpace(IgdbClientSecret);

    #endregion

    #region DeviantArt Properties

    private string _devClientId = "";
    public string DevClientId
    {
        get => _devClientId;
        set
        {
            if (SetProperty(ref _devClientId, value))
                ResetDeviantArtValidation();
        }
    }

    private string _devClientSecret = "";
    public string DevClientSecret
    {
        get => _devClientSecret;
        set
        {
            if (SetProperty(ref _devClientSecret, value))
                ResetDeviantArtValidation();
        }
    }

    private bool _isDeviantArtValidating;
    public bool IsDeviantArtValidating { get => _isDeviantArtValidating; set => SetProperty(ref _isDeviantArtValidating, value); }

    private bool? _isDeviantArtValid;
    public bool? IsDeviantArtValid { get => _isDeviantArtValid; set => SetProperty(ref _isDeviantArtValid, value); }

    private string _deviantArtValidationMessage = "";
    public string DeviantArtValidationMessage { get => _deviantArtValidationMessage; set => SetProperty(ref _deviantArtValidationMessage, value); }

    public bool IsDeviantArtConfigured => !string.IsNullOrWhiteSpace(DevClientId) && !string.IsNullOrWhiteSpace(DevClientSecret);

    #endregion

    #region Commands

    public DelegateCommand NextCommand { get; }
    public DelegateCommand BackCommand { get; }
    public DelegateCommand SkipCommand { get; }
    public DelegateCommand ValidateTmdbCommand { get; }
    public DelegateCommand ValidateIgdbCommand { get; }
    public DelegateCommand ValidateDeviantArtCommand { get; }
    public DelegateCommand<string> CloseDialogCommand { get; }

    #endregion

    private string _title = Lang.OnboardingTitle;
    public string Title { get => _title; set => SetProperty(ref _title, value); }

    public DialogCloseListener RequestClose { get; }

    public OnboardingWizardViewModel()
    {
        NextCommand = new DelegateCommand(ExecuteNext, CanExecuteNext)
            .ObservesProperty(() => CurrentStep);
        BackCommand = new DelegateCommand(ExecuteBack, () => CanGoBack)
            .ObservesProperty(() => CurrentStep);
        SkipCommand = new DelegateCommand(ExecuteSkip);
        ValidateTmdbCommand = new DelegateCommand(async () => await ValidateTmdbAsync());
        ValidateIgdbCommand = new DelegateCommand(async () => await ValidateIgdbAsync());
        ValidateDeviantArtCommand = new DelegateCommand(async () => await ValidateDeviantArtAsync());
        CloseDialogCommand = new DelegateCommand<string>(CloseDialog);

        // Pre-populate from existing config
        FileUtils.ReadApiConfiguration(out var tmdbKey, out var igdbClientId, out var igdbClientSecret,
            out var devClientSecret, out var devClientId);
        TmdbKey = tmdbKey ?? "";
        IgdbClientId = igdbClientId ?? "";
        IgdbClientSecret = igdbClientSecret ?? "";
        DevClientId = devClientId ?? "";
        DevClientSecret = devClientSecret ?? "";
    }

    private void ExecuteNext()
    {
        if (CurrentStep == 3)
        {
            // Finish — save and close
            SaveConfiguration();
            Services.Settings.OnboardingCompleted = true;
            Services.Settings.Save();
            CloseDialog("true");
            return;
        }

        CurrentStep++;
    }

    private bool CanExecuteNext() => CurrentStep < 4;
    private void ExecuteBack() => CurrentStep = Math.Max(0, CurrentStep - 1);

    private void ExecuteSkip()
    {
        // Skip means "don't configure this service right now" — advance without clearing fields.
        // Pre-populated values from existing config are preserved.
        // If the user deliberately cleared a field before hitting Skip, that change remains.
        CurrentStep++;
    }

    private void SaveConfiguration()
    {
        Logger.Info("Saving API configuration from onboarding wizard.");
        FileUtils.WriteApiConfiguration(TmdbKey ?? "", IgdbClientId ?? "", IgdbClientSecret ?? "",
            DevClientSecret ?? "", DevClientId ?? "");
        FileUtils.ClearCachedTokens();
    }

    #region Validation Methods

    private async Task ValidateTmdbAsync()
    {
        if (string.IsNullOrWhiteSpace(TmdbKey))
        {
            IsTmdbValid = false;
            TmdbValidationMessage = Lang.OnboardingValidationFailed + "API key cannot be empty.";
            return;
        }

        IsTmdbValidating = true;
        IsTmdbValid = null;
        TmdbValidationMessage = Lang.OnboardingValidating;

        try
        {
            var result = await ApiKeyValidator.ValidateTmdbKeyAsync(TmdbKey);
            IsTmdbValid = result.IsValid;
            TmdbValidationMessage = result.IsValid ? Lang.OnboardingValidationSuccess : result.Message;
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Error during TMDB validation.");
            IsTmdbValid = false;
            TmdbValidationMessage = Lang.OnboardingValidationFailed + ex.Message;
        }
        finally
        {
            IsTmdbValidating = false;
        }
    }

    private async Task ValidateIgdbAsync()
    {
        if (string.IsNullOrWhiteSpace(IgdbClientId) || string.IsNullOrWhiteSpace(IgdbClientSecret))
        {
            IsIgdbValid = false;
            IgdbValidationMessage = Lang.OnboardingValidationFailed + "Client ID and Secret cannot be empty.";
            return;
        }

        IsIgdbValidating = true;
        IsIgdbValid = null;
        IgdbValidationMessage = Lang.OnboardingValidating;

        try
        {
            var result = await ApiKeyValidator.ValidateIgdbCredentialsAsync(IgdbClientId, IgdbClientSecret);
            IsIgdbValid = result.IsValid;
            IgdbValidationMessage = result.IsValid ? Lang.OnboardingValidationSuccess : result.Message;
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Error during IGDB validation.");
            IsIgdbValid = false;
            IgdbValidationMessage = Lang.OnboardingValidationFailed + ex.Message;
        }
        finally
        {
            IsIgdbValidating = false;
        }
    }

    private async Task ValidateDeviantArtAsync()
    {
        if (string.IsNullOrWhiteSpace(DevClientId) || string.IsNullOrWhiteSpace(DevClientSecret))
        {
            IsDeviantArtValid = false;
            DeviantArtValidationMessage = Lang.OnboardingValidationFailed + "Client ID and Secret cannot be empty.";
            return;
        }

        IsDeviantArtValidating = true;
        IsDeviantArtValid = null;
        DeviantArtValidationMessage = Lang.OnboardingValidating;

        try
        {
            var result = await ApiKeyValidator.ValidateDeviantArtCredentialsAsync(DevClientId, DevClientSecret);
            IsDeviantArtValid = result.IsValid;
            DeviantArtValidationMessage = result.IsValid ? Lang.OnboardingValidationSuccess : result.Message;
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Error during DeviantArt validation.");
            IsDeviantArtValid = false;
            DeviantArtValidationMessage = Lang.OnboardingValidationFailed + ex.Message;
        }
        finally
        {
            IsDeviantArtValidating = false;
        }
    }

    private void ResetTmdbValidation()
    {
        if (IsTmdbValid != null) { IsTmdbValid = null; TmdbValidationMessage = ""; }
        RaisePropertyChanged(nameof(IsTmdbConfigured));
    }

    private void ResetIgdbValidation()
    {
        if (IsIgdbValid != null) { IsIgdbValid = null; IgdbValidationMessage = ""; }
        RaisePropertyChanged(nameof(IsIgdbConfigured));
    }

    private void ResetDeviantArtValidation()
    {
        if (IsDeviantArtValid != null) { IsDeviantArtValid = null; DeviantArtValidationMessage = ""; }
        RaisePropertyChanged(nameof(IsDeviantArtConfigured));
    }

    #endregion

    #region IDialogAware

    public virtual bool CanCloseDialog() => true;

    public virtual void OnDialogClosed() { }

    public virtual void OnDialogOpened(IDialogParameters parameters)
    {
        CurrentStep = 0;
    }

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

    #endregion
}
