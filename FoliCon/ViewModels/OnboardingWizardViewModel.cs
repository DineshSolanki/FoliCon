using FoliCon.Modules.Validation;

namespace FoliCon.ViewModels;

public class OnboardingWizardViewModel : BindableBase, IDialogAware
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
    
    public static string TmdbPortalUrl => TmdbAppConfig.ApiKeyPortalUrl;
    public static string IgdbPortalUrl => IgdbAppConfig.CredentialsPortalUrl;

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

    public bool IsDeviantArtConnected
    {
        get;
        set
        {
            if (!SetProperty(ref field, value)) return;
            RaisePropertyChanged(nameof(IsDeviantArtConfigured));
            RaisePropertyChanged(nameof(DeviantArtConnectionStatus));
        }
    }

    private bool _isDeviantArtConnecting;
    public bool IsDeviantArtConnecting { get => _isDeviantArtConnecting; set => SetProperty(ref _isDeviantArtConnecting, value); }

    private string _deviantArtConnectionMessage = "";
    public string DeviantArtConnectionMessage
    {
        get => _deviantArtConnectionMessage;
        set
        {
            if (SetProperty(ref _deviantArtConnectionMessage, value))
                RaisePropertyChanged(nameof(HasDeviantArtConnectionMessage));
        }
    }

    public bool HasDeviantArtConnectionMessage => !string.IsNullOrEmpty(DeviantArtConnectionMessage);

    public bool IsDeviantArtConfigured => IsDeviantArtConnected;

    public string DeviantArtConnectionStatus =>
        IsDeviantArtConnecting ? Lang.OnboardingDeviantArtConnecting :
        IsDeviantArtConnected ? Lang.OnboardingDeviantArtConnected :
        Lang.OnboardingDeviantArtNotConnected;

    private bool _isSyncingMode;

    public bool IsDeviantArtCustomMode
    {
        get;
        set
        {
            if (!SetProperty(ref field, value)) return;
            RaisePropertyChanged(nameof(IsDeviantArtCustomCredentialsVisible));
            if (_isSyncingMode) return;
            _isSyncingMode = true;
            IsDeviantArtBuiltInMode = !value;
            _isSyncingMode = false;
        }
    }

    private bool _isDeviantArtBuiltInMode = true;
    public bool IsDeviantArtBuiltInMode
    {
        get => _isDeviantArtBuiltInMode;
        set
        {
            if (!SetProperty(ref _isDeviantArtBuiltInMode, value)) return;
            if (_isSyncingMode) return;
            _isSyncingMode = true;
            IsDeviantArtCustomMode = !value;
            _isSyncingMode = false;
        }
    }

    public bool IsDeviantArtCustomCredentialsVisible => IsDeviantArtCustomMode;

    private string _deviantArtCustomClientId = "";
    public string DeviantArtCustomClientId
    {
        get => _deviantArtCustomClientId;
        set => SetProperty(ref _deviantArtCustomClientId, value);
    }

    private string _deviantArtCustomClientSecret = "";
    public string DeviantArtCustomClientSecret
    {
        get => _deviantArtCustomClientSecret;
        set => SetProperty(ref _deviantArtCustomClientSecret, value);
    }

    #endregion

    #region Commands

    public DelegateCommand NextCommand { get; }
    public DelegateCommand BackCommand { get; }
    public DelegateCommand SkipCommand { get; }
    public DelegateCommand ValidateTmdbCommand { get; }
    public DelegateCommand ValidateIgdbCommand { get; }
    public DelegateCommand ConnectDeviantArtCommand { get; }
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
        ConnectDeviantArtCommand = new DelegateCommand(async () => await ConnectDeviantArtAsync());
        CloseDialogCommand = new DelegateCommand<string>(CloseDialog);

        // Pre-populate TMDB + IGDB from existing config
        FileUtils.ReadApiConfiguration(out var tmdbKey, out var igdbClientId, out var igdbClientSecret);
        TmdbKey = tmdbKey ?? "";
        IgdbClientId = igdbClientId ?? "";
        IgdbClientSecret = igdbClientSecret ?? "";

        // Check if DeviantArt is already connected (has stored tokens)
        IsDeviantArtConnected = !string.IsNullOrEmpty(Services.Settings.DeviantArtAccessToken);

        // Pre-populate DeviantArt custom credentials if set
        DeviantArtCustomClientId = Services.Settings.DeviantArtClientId ?? "";
        DeviantArtCustomClientSecret = Services.Settings.DeviantArtClientSecret ?? "";
        IsDeviantArtCustomMode = !string.IsNullOrEmpty(DeviantArtCustomClientId);
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
        CurrentStep++;
    }

    private void SaveConfiguration()
    {
        Logger.Info("Saving API configuration from onboarding wizard.");
        FileUtils.WriteApiConfiguration(TmdbKey ?? "", IgdbClientId ?? "", IgdbClientSecret ?? "");
        // DeviantArt tokens are already persisted by DArt.AuthorizeAsync() during the Connect step
        FileUtils.ClearCachedTokens();
    }

    #region Validation & Connection Methods

    private async Task ValidateTmdbAsync()
    {
        if (string.IsNullOrWhiteSpace(TmdbKey))
        {
            IsTmdbValid = false;
            TmdbValidationMessage = Lang.OnboardingValidationFailed + Lang.ApiKeyCannotBeEmpty;
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
            IgdbValidationMessage = Lang.OnboardingValidationFailed + Lang.ClientIdAndSecretCannotBeEmpty;
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

    private async Task ConnectDeviantArtAsync()
    {
        IsDeviantArtConnecting = true;
        DeviantArtConnectionMessage = Lang.OnboardingDeviantArtConnecting;

        try
        {
            if (IsDeviantArtCustomMode)
            {
                // client_credentials flow — no browser needed
                Services.Settings.DeviantArtClientId = DeviantArtCustomClientId;
                Services.Settings.DeviantArtClientSecret = DeviantArtCustomClientSecret;
                Logger.Info("Using custom DeviantArt credentials (client_id={ClientId})", DeviantArtCustomClientId);
                await DArt.AuthorizeWithCredentialsAsync(DeviantArtCustomClientId, DeviantArtCustomClientSecret);
            }
            else
            {
                // OAuth PKCE flow — browser-based
                Services.Settings.DeviantArtClientId = "";
                Services.Settings.DeviantArtClientSecret = "";
                Logger.Info("Initiating DeviantArt OAuth PKCE authorization from wizard");
                await DArt.AuthorizeAsync();
            }

            IsDeviantArtConnected = true;
            DeviantArtConnectionMessage = Lang.OnboardingDeviantArtConnected;
            Logger.Info("DeviantArt authorization successful");
        }
        catch (TimeoutException ex)
        {
            Logger.Warn(ex, "DeviantArt authorization timed out");
            IsDeviantArtConnected = false;
            DeviantArtConnectionMessage = Lang.OnboardingDeviantArtConnectFailed + " " + Lang.OnboardingNetworkError;
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "DeviantArt authorization failed");
            IsDeviantArtConnected = false;
            DeviantArtConnectionMessage = Lang.OnboardingDeviantArtConnectFailed + " " + ex.Message;
        }
        finally
        {
            IsDeviantArtConnecting = false;
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
