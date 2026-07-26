using Prism.Ioc;
using Sentry;
using Window = System.Windows.Window;

namespace FoliCon;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
[Localizable(false)]
public partial class App
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
    protected override Window CreateShell()
    {
        // Ensure GlobalVariables uses the DI-registered singleton (not a separate instance)
        GlobalVariables.SetOverlayProvider(Container.Resolve<IOverlayProvider>());

        var shell = Container.Resolve<MainWindow>();

        // Fire-and-forget overlay update check on app start
        _ = CheckOverlayUpdatesAsync();

        return shell;
    }

    private async Task CheckOverlayUpdatesAsync()
    {
        try
        {
            var checker = Container.Resolve<OverlayUpdateChecker>();
            await checker.CheckForUpdatesAsync();
        }
        catch (Exception ex)
        {
            Logger.Warn(ex, "Overlay update check failed during startup");
        }
    }

    public App()
    {

        LogManager.Configuration = LogUtils.GetNLogConfig();
        DispatcherUnhandledException += App_DispatcherUnhandledException;
        GlobalDataHelper.Load<AppConfig>();
        Logger.Info("FoliCon Initialized");
        AppDomain.CurrentDomain.SetData("REGEX_DEFAULT_MATCH_TIMEOUT", TimeSpan.FromMilliseconds(1000));
    }

    protected override void RegisterTypes(IContainerRegistry containerRegistry)
    {
        containerRegistry.RegisterDialog<DialogControl, DialogControlViewModel>("MessageBox");
        containerRegistry.RegisterDialog<SearchResult, SearchResultViewModel>("SearchResult");
        containerRegistry.RegisterDialog<ProSearchResult, ProSearchResultViewModel>("ProSearchResult");
        containerRegistry.RegisterDialog<CustomIconControl, CustomIconControlViewModel>("CustomIcon");
        containerRegistry.RegisterDialog<PosterIconConfig, PosterIconConfigViewModel>("PosterIconConfig");
        containerRegistry.RegisterDialog<SubfolderProcessing, SubfolderProcessingViewModel>("SubfolderProcessingConfig");
        containerRegistry.RegisterDialog<ManualExplorer, ManualExplorerViewModel>("ManualExplorer");
        containerRegistry.RegisterDialog<AboutBox, AboutBoxViewModel>("AboutBox");
        containerRegistry.RegisterDialog<PosterPicker, PosterPickerViewModel>("PosterPicker");
        containerRegistry.RegisterDialog<Previewer, PreviewerViewModel>("Previewer");
        containerRegistry.RegisterDialog<OnboardingWizard, OnboardingWizardViewModel>("OnboardingWizard");
        containerRegistry.RegisterDialogWindow<HandyWindow>();

        // Overlay plugin system
        containerRegistry.RegisterSingleton<IOverlayProvider, OverlayProvider>();
        containerRegistry.RegisterSingleton<IOverlayRepositoryService, OverlayRepositoryService>();
        containerRegistry.RegisterSingleton<OverlayUpdateChecker>();
        containerRegistry.RegisterDialog<OverlayStore, OverlayStoreViewModel>("OverlayStore");
        containerRegistry.RegisterDialog<OverlayDesigner, OverlayDesignerViewModel>("OverlayDesigner");
    }

    private static void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        Logger.Fatal(e.Exception, "Unhandled exception caught by DispatcherUnhandledException handler");
        SentrySdk.CaptureException(e.Exception);

        // If you want to avoid the application from crashing:
        e.Handled = true;
    }
}
