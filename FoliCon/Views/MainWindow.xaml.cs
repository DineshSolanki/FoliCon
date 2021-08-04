using FoliCon.Modules;
using System.Collections.Specialized;
using System.Globalization;
using System.Threading;
using System.Windows.Controls;
using FoliCon.Models;
using FoliCon.Properties.Langs;
using HandyControl.Tools;
using Vanara.PInvoke;

namespace FoliCon.Views
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        public MainWindow()
        {
            InitializeComponent();
            ((INotifyCollectionChanged)FinalList.Items).CollectionChanged += ListView_CollectionChanged;
        }

        private void ListView_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
        }

        private void ListView_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            Util.SetColumnWidth(FinalList);
            if (e.Action == NotifyCollectionChangedAction.Reset)
            {
                //    // scroll the new item into view

                //    //FinalList.ScrollIntoView(e.NewItems[0]);
            }
        }

        private void CmbLanguage_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {

            if (CmbLanguage.SelectedItem is null)
                return;
            var selectedLanguage = (Languages)CmbLanguage.SelectedValue;
            switch (selectedLanguage)
            {
                case Languages.English:
                    ConfigHelper.Instance.SetLang("en");
                    LangProvider.Culture = new CultureInfo("en-US");
                    break;
                case Languages.Spanish:
                    ConfigHelper.Instance.SetLang("es");
                    LangProvider.Culture = new CultureInfo("es-MX");
                    break;
                case Languages.Arabic:
                    ConfigHelper.Instance.SetLang("ar");
                    LangProvider.Culture = new CultureInfo("ar-SA");
                    break;
                case Languages.Russian:
                    ConfigHelper.Instance.SetLang("ru");
                    LangProvider.Culture = new CultureInfo("ru-RU");
                    break;
                case Languages.Hindi:
                    ConfigHelper.Instance.SetLang("hi");
                    LangProvider.Culture = new CultureInfo("hi-IN");
                    break;
                default:
                    ConfigHelper.Instance.SetLang("en");
                    LangProvider.Culture = new CultureInfo("en-US");
                    break;
            }
            Thread.CurrentThread.CurrentCulture = LangProvider.Culture;
            Thread.CurrentThread.CurrentUICulture = LangProvider.Culture;
            Kernel32.SetThreadUILanguage((ushort)Thread.CurrentThread.CurrentUICulture.LCID);
            if (FinalList is not null)
            {
                Util.SetColumnWidth(FinalList);
            }

        }
    }
}