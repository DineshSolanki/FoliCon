using FoliCon.Modules;
using FoliCon.ViewModels;
using FoliCon.Views;
using HandyControl.Tools;
using Prism.Ioc;

namespace FoliCon
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App
    {
        protected override System.Windows.Window CreateShell()
        {
            return Container.Resolve<MainWindow>();
        }

        public App()
        {
            GlobalDataHelper.Load<AppConfig>();
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
        }
    }
}