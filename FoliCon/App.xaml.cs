using FoliCon.Modules;
using FoliCon.ViewModels;
using FoliCon.Views;
using HandyControl.Controls;
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
            GlobalDataHelper<AppConfig>.Init();
        }

        protected override void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterDialog<DialogControl, DialogControlViewModel>("MessageBox");
            containerRegistry.RegisterDialog<SearchResult, SearchResultViewModel>("SearchResult");
            containerRegistry.RegisterDialog<ProSearchResult, ProSearchResultViewModel>("ProSearchResult");
            containerRegistry.RegisterDialog<ApiConfiguration, ApiConfigurationViewModel>("ApiConfig");
            containerRegistry.RegisterDialog<posterIconConfig, PosterIconConfigViewModel>("PosterIconConfig");
            containerRegistry.RegisterDialog<AboutBox, AboutBoxViewModel>("AboutBox");
        }
    }
}