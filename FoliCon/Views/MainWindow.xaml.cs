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
            var cultureInfo = Util.GetCultureInfoByLanguage(selectedLanguage);
            LangProvider.Culture = cultureInfo;
            Thread.CurrentThread.CurrentCulture = cultureInfo;
            Thread.CurrentThread.CurrentUICulture = cultureInfo;
            Kernel32.SetThreadUILanguage((ushort)Thread.CurrentThread.CurrentUICulture.LCID);
            if (FinalList is not null)
            {
                Util.SetColumnWidth(FinalList);
            }

        }
    }
}