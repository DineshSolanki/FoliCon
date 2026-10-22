using System.Windows.Input;
using FoliCon.Models.Localization;
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
    private ThemeOption? _lastHoveredOption;

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
            vm.IsPreviewingTheme = true;
        }
        _isDropdownOpen = true;
        _isCommitted = false;
        _lastHoveredOption = null;
        _dropdownPreviewSnapshot ??= MainWindowViewModel.CaptureThemeSnapshot();
    }

    private void TriggerThemeHoverPreview(object? source)
    {
        var depObj = source as DependencyObject;
        var item = FindVisualParent<ComboBoxItem>(depObj);
        var option = item?.DataContext as ThemeOption
                  ?? (depObj as FrameworkElement)?.DataContext as ThemeOption;

        if (option != null && DataContext is MainWindowViewModel vm)
        {
            if (!ReferenceEquals(_lastHoveredOption, option))
            {
                _lastHoveredOption = option;
                _isDropdownOpen = true;
                _dropdownPreviewSnapshot ??= MainWindowViewModel.CaptureThemeSnapshot();
                vm.IsPreviewingTheme = true;
                vm.ApplyThemeOption(option, isPreview: true);
            }
        }
    }

    private void ThemeItem_OnMouseEnter(object sender, MouseEventArgs e)
    {
        TriggerThemeHoverPreview(sender ?? e.OriginalSource);
    }

    private void ThemeItem_OnPreviewMouseMove(object sender, MouseEventArgs e)
    {
        TriggerThemeHoverPreview(sender ?? e.OriginalSource);
    }

    private void ComboBoxItem_OnMouseEnter(object sender, MouseEventArgs e)
    {
        TriggerThemeHoverPreview(sender ?? e.OriginalSource);
    }

    private void ComboBoxItem_OnPreviewMouseMove(object sender, MouseEventArgs e)
    {
        TriggerThemeHoverPreview(sender ?? e.OriginalSource);
    }

    private void CmbTheme_OnPreviewMouseMove(object sender, MouseEventArgs e)
    {
        if (sender is WpfComboBox combo && combo.IsDropDownOpen)
        {
            TriggerThemeHoverPreview(e.OriginalSource);
        }
    }

    private static T? FindVisualParent<T>(DependencyObject? child) where T : DependencyObject
    {
        while (child != null)
        {
            if (child is T parent) return parent;
            child = child is Visual or System.Windows.Media.Media3D.Visual3D
                ? VisualTreeHelper.GetParent(child)
                : LogicalTreeHelper.GetParent(child);
        }
        return null;
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
            _lastHoveredOption = null;
            if (combo.SelectedItem is ThemeOption option && DataContext is MainWindowViewModel vm)
            {
                vm.IsPreviewingTheme = false;
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
        _lastHoveredOption = null;
        if (DataContext is MainWindowViewModel vm)
        {
            vm.IsPreviewingTheme = false;
        }

        if (!_isCommitted)
        {
            RollbackDropdownPreview();
        }
        else
        {
            if (sender is WpfComboBox combo && combo.SelectedItem is ThemeOption option && DataContext is MainWindowViewModel vmCommit)
            {
                vmCommit.ApplyThemeOption(option, isPreview: false);
            }
        }
        _dropdownPreviewSnapshot = null;
        _isCommitted = false;
    }

    private void RollbackDropdownPreview()
    {
        _lastHoveredOption = null;
        if (_dropdownPreviewSnapshot != null)
        {
            var savedCustomId = _dropdownPreviewSnapshot.ActiveThemeManifest != null && !_dropdownPreviewSnapshot.ActiveThemeManifest.IsBuiltIn
                ? _dropdownPreviewSnapshot.ActiveThemeManifest.Id
                : string.Empty;

            MainWindowViewModel.RestoreThemeSnapshot(_dropdownPreviewSnapshot);
            _dropdownPreviewSnapshot = null;

            if (DataContext is MainWindowViewModel vm)
            {
                vm.IsPreviewingTheme = false;
                vm.CustomThemeId = savedCustomId;
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

        Languages selectedLanguage;
        if (CmbLanguage.SelectedItem is LanguageOption opt)
        {
            selectedLanguage = opt.Language;
            if (DataContext is MainWindowViewModel vm && vm.AppLanguage != selectedLanguage)
            {
                vm.AppLanguage = selectedLanguage;
            }
        }
        else if (CmbLanguage.SelectedItem is Languages lang)
        {
            selectedLanguage = lang;
            if (DataContext is MainWindowViewModel vm && vm.AppLanguage != selectedLanguage)
            {
                vm.AppLanguage = selectedLanguage;
            }
        }
        else
        {
            return;
        }

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

    private void TopLanguageComboBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is WpfComboBox combo && combo.SelectedItem is LanguageOption option)
        {
            if (DataContext is MainWindowViewModel vm && vm.AppLanguage != option.Language)
            {
                vm.AppLanguage = option.Language;
            }

            var cultureInfo = CultureUtils.GetCultureInfoByLanguage(option.Language);
            LangProvider.Culture = cultureInfo;
            Thread.CurrentThread.CurrentCulture = cultureInfo;
            Thread.CurrentThread.CurrentUICulture = cultureInfo;
            Kernel32.SetThreadUILanguage((ushort)Thread.CurrentThread.CurrentUICulture.LCID);
            if (FinalList is not null)
            {
                UiUtils.SetColumnWidth(FinalList);
            }
        }
    }

    private void MainWindow_OnClosed(object sender, EventArgs e)
    {
        RollbackDropdownPreview();
        FileUtils.DeleteFoliConTempDeviationDirectory();
    }
}
