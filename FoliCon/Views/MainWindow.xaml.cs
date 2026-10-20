namespace FoliCon.Views;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow
{
    public MainWindow()
    {
        InitializeComponent();
        WindowBackdropHelper.SuppressNativeCaptionButtons(this);
        WindowBackdropHelper.UpdateImmersiveDarkMode(this, MainWindowViewModel.IsCurrentThemeDark());
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        WindowBackdropHelper.SuppressNativeCaptionButtons(this);
        WindowBackdropHelper.UpdateImmersiveDarkMode(this, MainWindowViewModel.IsCurrentThemeDark());
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

    private void MainWindow_OnClosed(object sender, EventArgs e) => FileUtils.DeleteFoliConTempDeviationDirectory();
}