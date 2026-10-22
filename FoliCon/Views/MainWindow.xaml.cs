using System.Windows.Input;
using FoliCon.Models.Themes;
using FoliCon.Modules.Themes;
using WpfComboBox = System.Windows.Controls.ComboBox;

namespace FoliCon.Views;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow
{
    private ThemeStateSnapshot? _dropdownPreviewSnapshot;
    private bool _isDropdownOpen;
    private bool _isCommitted;

    public MainWindow()
    {
        InitializeComponent();
        WindowBackdropHelper.SuppressNativeCaptionButtons(this);
        WindowBackdropHelper.UpdateImmersiveDarkMode(this, MainWindowViewModel.IsCurrentThemeDark());

        PreviewKeyDown += MainWindow_OnPreviewKeyDown;
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        WindowBackdropHelper.SuppressNativeCaptionButtons(this);
        WindowBackdropHelper.UpdateImmersiveDarkMode(this, MainWindowViewModel.IsCurrentThemeDark());
    }

    private void MainWindow_OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape && _isDropdownOpen)
        {
            // Escape pressed while previewing themes -> cancel & rollback
            RollbackDropdownPreview();
        }
    }

    private void CmbTheme_OnDropDownOpened(object sender, EventArgs e)
    {
        if (DataContext is MainWindowViewModel vm)
        {
            vm.RefreshAvailableThemes();
        }
        _isDropdownOpen = true;
        _isCommitted = false;
        _dropdownPreviewSnapshot ??= MainWindowViewModel.CaptureThemeSnapshot();
    }

    private void CmbTheme_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isDropdownOpen && sender is WpfComboBox combo)
        {
            _dropdownPreviewSnapshot ??= MainWindowViewModel.CaptureThemeSnapshot();
            if (combo.SelectedItem is ThemeOption option && DataContext is MainWindowViewModel vm)
            {
                vm.ApplyThemeOption(option, isPreview: true);
            }
            else if (combo.SelectedItem is FoliconThemes theme)
            {
                MainWindowViewModel.ApplyAppTheme(theme);
            }
        }
    }

    private void CmbTheme_OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (sender is not WpfComboBox combo) return;

        if (e.Key == Key.Down)
        {
            e.Handled = true;
            if (!_isDropdownOpen)
            {
                combo.IsDropDownOpen = true;
            }
            if (combo.Items.Count > 0)
            {
                var nextIndex = combo.SelectedIndex < combo.Items.Count - 1 ? combo.SelectedIndex + 1 : 0;
                combo.SelectedIndex = nextIndex;
            }
        }
        else if (e.Key == Key.Up)
        {
            e.Handled = true;
            if (!_isDropdownOpen)
            {
                combo.IsDropDownOpen = true;
            }
            if (combo.Items.Count > 0)
            {
                var prevIndex = combo.SelectedIndex > 0 ? combo.SelectedIndex - 1 : combo.Items.Count - 1;
                combo.SelectedIndex = prevIndex;
            }
        }
        else if (e.Key is Key.Enter or Key.Tab)
        {
            e.Handled = true;
            _isCommitted = true;
            combo.IsDropDownOpen = false;
            _dropdownPreviewSnapshot = null;
            if (combo.SelectedItem is ThemeOption option && DataContext is MainWindowViewModel vm)
            {
                vm.ApplyThemeOption(option, isPreview: false);
            }
        }
        else if (e.Key == Key.Escape)
        {
            e.Handled = true;
            combo.IsDropDownOpen = false;
            RollbackDropdownPreview();
        }
    }

    private void ComboBoxItem_OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _isCommitted = true;
    }

    private void CmbTheme_OnDropDownClosed(object sender, EventArgs e)
    {
        _isDropdownOpen = false;
        if (!_isCommitted)
        {
            RollbackDropdownPreview();
        }
        _dropdownPreviewSnapshot = null;
    }

    private void RollbackDropdownPreview()
    {
        if (_dropdownPreviewSnapshot != null)
        {
            MainWindowViewModel.RestoreThemeSnapshot(_dropdownPreviewSnapshot);
            _dropdownPreviewSnapshot = null;

            if (DataContext is MainWindowViewModel vm)
            {
                vm.SyncSelectedThemeOptionWithActive();
            }
        }
        _isDropdownOpen = false;
        _isCommitted = false;
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
        RollbackDropdownPreview();
        FileUtils.DeleteFoliConTempDeviationDirectory();
    }
}
