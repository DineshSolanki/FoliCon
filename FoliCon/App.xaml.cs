using FoliCon.Modules.Configuration;
using FoliCon.Modules.UI;
using FoliCon.Modules.utils;
using FoliCon.ViewModels;
using NLog;
using Prism.Ioc;
using Sentry;
using Logger = NLog.Logger;

namespace FoliCon;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
    protected override System.Windows.Window CreateShell()
    {
        return Container.Resolve<MainWindow>();
    }

    public App()
    {
        
        LogManager.Configuration = LogUtils.GetNLogConfig();
        DispatcherUnhandledException += App_DispatcherUnhandledException;
        GlobalDataHelper.Load<AppConfig>();
        Plugins.PluginManager.Instance.Initialize();
        Logger.Info("FoliCon Initilized");
    }

    protected override void RegisterTypes(IContainerRegistry containerRegistry)
    {
        containerRegistry.RegisterDialog<DialogControl, DialogControlViewModel>("MessageBox");
        containerRegistry.RegisterDialog<SearchResult, SearchResultViewModel>("SearchResult");
        containerRegistry.RegisterDialog<ProSearchResult, ProSearchResultViewModel>("ProSearchResult");
        containerRegistry.RegisterDialog<ApiConfiguration, ApiConfigurationViewModel>("ApiConfig");
        containerRegistry.RegisterDialog<CustomIconControl, CustomIconControlViewModel>("CustomIcon");
        containerRegistry.RegisterDialog<PosterIconConfig, PosterIconConfigViewModel>("PosterIconConfig");
        containerRegistry.RegisterDialog<AboutBox, AboutBoxViewModel>("AboutBox");
        containerRegistry.RegisterDialog<PosterPicker, PosterPickerViewModel>("PosterPicker");
        containerRegistry.RegisterDialog<Previewer, PreviewerViewModel>("Previewer");
        containerRegistry.RegisterDialogWindow<HandyWindow>();
    }
    
    void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        SentrySdk.CaptureException(e.Exception);

        // If you want to avoid the application from crashing:
        e.Handled = true;
    }
}