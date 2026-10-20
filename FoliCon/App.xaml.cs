using System;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using FoliCon.Models.Configs;
using FoliCon.Models.Constants;
using FoliCon.Modules.IPC;
using FoliCon.Modules.Overlays;
using FoliCon.Modules.Protocol;
using FoliCon.Modules.Themes;
using FoliCon.Modules.UI;
using FoliCon.Modules.utils;
using FoliCon.ViewModels;
using FoliCon.Views;
using NLog;
using Prism.Dialogs;
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
    private ISingleInstanceIpcService? _ipcService;

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

    protected override void OnStartup(StartupEventArgs e)
    {
        _ipcService = new SingleInstanceIpcService();
        if (!_ipcService.CheckIsFirstInstance())
        {
            Logger.Info("Secondary FoliCon instance detected. Forwarding arguments to running instance...");
            var args = Environment.GetCommandLineArgs().Skip(1).ToArray();
            _ = _ipcService.SendArgsToFirstInstanceAsync(args).GetAwaiter().GetResult();
            _ipcService.Dispose();
            Environment.Exit(0);
            return;
        }

        base.OnStartup(e);
    }

    protected override void OnInitialized()
    {
        base.OnInitialized();

        // 1. Start Named Pipe IPC server in running instance
        _ipcService?.StartServer(jsonArgs =>
        {
            try
            {
                var args = System.Text.Json.JsonSerializer.Deserialize<string[]>(jsonArgs);
                if (args != null)
                {
                    foreach (var arg in args)
                    {
                        if (arg.StartsWith("folicon://", StringComparison.OrdinalIgnoreCase))
                        {
                            var dispatcher = Container.Resolve<IDeepLinkDispatcher>();
                            var dialogService = Container.Resolve<IDialogService>();
                            var request = dispatcher.Parse(arg);
                            _ = dispatcher.DispatchAsync(request, dialogService);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Warn(ex, "Failed to process IPC message in primary instance");
            }
        });

        // 2. Ensure folicon:// protocol scheme is registered in HKCU
        try
        {
            var protocolService = Container.Resolve<IProtocolRegistrationService>();
            if (!protocolService.IsRegistered())
            {
                protocolService.RegisterProtocol();
            }
        }
        catch (Exception ex)
        {
            Logger.Warn(ex, "Failed to verify or register folicon:// protocol handler");
        }

        // 3. Process initial deeplink if passed when launching first instance
        try
        {
            var cmdArgs = ProcessUtils.GetCmdArgs();
            if (cmdArgs.TryGetValue("deeplink", out var deeplink))
            {
                var dispatcher = Container.Resolve<IDeepLinkDispatcher>();
                var dialogService = Container.Resolve<IDialogService>();
                var request = dispatcher.Parse(deeplink);
                _ = dispatcher.DispatchAsync(request, dialogService);
            }
        }
        catch (Exception ex)
        {
            Logger.Warn(ex, "Failed to process initial deeplink argument");
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _ipcService?.Dispose();
        base.OnExit(e);
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

        // Custom Community Theme system
        containerRegistry.RegisterSingleton<IThemeParser, HybridThemeParser>();
        containerRegistry.RegisterSingleton<IThemeStorageService, ThemeStorageService>();
        containerRegistry.RegisterSingleton<IThemeRepositoryService, ThemeRepositoryService>();

        // Universal Deeplink & Single-Instance IPC system
        containerRegistry.RegisterSingleton<ISingleInstanceIpcService, SingleInstanceIpcService>();
        containerRegistry.RegisterSingleton<IProtocolRegistrationService, ProtocolRegistrationService>();
        containerRegistry.RegisterSingleton<IDeepLinkDispatcher, DeepLinkDispatcher>();
        containerRegistry.RegisterDialog<DeepLinkInstallDialog, DeepLinkInstallDialogViewModel>("DeepLinkInstallDialog");

        // Overlay & Unified Community Store plugin system
        containerRegistry.RegisterSingleton<IOverlayProvider, OverlayProvider>();
        containerRegistry.RegisterSingleton<IOverlayRepositoryService, OverlayRepositoryService>();
        containerRegistry.RegisterSingleton<OverlayUpdateChecker>();
        containerRegistry.RegisterDialog<FoliConStore, FoliConStoreViewModel>("FoliConStore");
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
