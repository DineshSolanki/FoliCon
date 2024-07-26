using FoliCon.Models.Enums;
using FoliCon.Modules.utils;

namespace FoliCon.Views;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow
{
    public MainWindow()
    {
        InitializeComponent();
    }
    

    private void CmbLanguage_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {

        if (CmbLanguage.SelectedItem is null)
        {
            return;
        }

        var selectedLanguage = (Languages)CmbLanguage.SelectedValue;
        var cultureInfo = CultureUtils.GetCultureInfoByLanguage(selectedLanguage);
        LangProvider.Culture = cultureInfo;
        Thread.CurrentThread.CurrentCulture = cultureInfo;
        Thread.CurrentThread.CurrentUICulture = cultureInfo;
        Kernel32.SetThreadUILanguage((ushort)Thread.CurrentThread.CurrentUICulture.LCID);
        if (FinalList is not null)
        {
            UiUtils.SetColumnWidth(FinalList);
        }

    }

    private void MainWindow_OnClosed(object sender, EventArgs e)
    {
        FileUtils.DeleteFoliConTempDeviationDirectory();
    }
}