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
        return Container.Resolve<MainWindow>();
    }

    public App()
    {
        
        LogManager.Configuration = LogUtils.GetNLogConfig();
        DispatcherUnhandledException += App_DispatcherUnhandledException;
        GlobalDataHelper.Load<AppConfig>();
        Logger.Info("FoliCon Initialized");
        AppDomain.CurrentDomain.SetData("REGEX_DEFAULT_MATCH_TIMEOUT", TimeSpan.FromMilliseconds(100));
    }

    protected override void RegisterTypes(IContainerRegistry containerRegistry)
    {
        containerRegistry.RegisterDialog<DialogControl, DialogControlViewModel>("MessageBox");
        containerRegistry.RegisterDialog<SearchResult, SearchResultViewModel>("SearchResult");
        containerRegistry.RegisterDialog<ProSearchResult, ProSearchResultViewModel>("ProSearchResult");
        containerRegistry.RegisterDialog<ApiConfiguration, ApiConfigurationViewModel>("ApiConfig");
        containerRegistry.RegisterDialog<CustomIconControl, CustomIconControlViewModel>("CustomIcon");
        containerRegistry.RegisterDialog<PosterIconConfig, PosterIconConfigViewModel>("PosterIconConfig");
        containerRegistry.RegisterDialog<SubfolderProcessing, SubfolderProcessingViewModel>("SubfolderProcessingConfig");
        containerRegistry.RegisterDialog<ManualExplorer, ManualExplorerViewModel>("ManualExplorer");
        containerRegistry.RegisterDialog<AboutBox, AboutBoxViewModel>("AboutBox");
        containerRegistry.RegisterDialog<PosterPicker, PosterPickerViewModel>("PosterPicker");
        containerRegistry.RegisterDialog<Previewer, PreviewerViewModel>("Previewer");
        containerRegistry.RegisterDialogWindow<HandyWindow>();
    }

    private static void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        SentrySdk.CaptureException(e.Exception);

        // If you want to avoid the application from crashing:
        e.Handled = true;
    }
}