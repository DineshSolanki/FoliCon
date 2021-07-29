using System;
using FoliCon.Modules;
using System.Collections.Specialized;
using System.Globalization;
using System.Windows.Controls;
using FoliCon.Models;
using FoliCon.Properties.Langs;
using HandyControl.Tools;

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
                    LangProvider.Culture = new CultureInfo("en");
                    break;
                case Languages.Spanish:
                    ConfigHelper.Instance.SetLang("es");
                    LangProvider.Culture = new CultureInfo("es");
                    break;
                case Languages.Arabic:
                    ConfigHelper.Instance.SetLang("ar");
                    LangProvider.Culture = new CultureInfo("ar");
                    break;
                case Languages.Russian:
                    break;
                default:
                    ConfigHelper.Instance.SetLang("ru");
                    LangProvider.Culture = new CultureInfo("ru");
                    break;
            }

        }
    }
}