#nullable enable
using System.Windows;
using System.Windows.Input;
using Brush = System.Windows.Media.Brush;
using Brushes = System.Windows.Media.Brushes;

namespace FoliCon.Views;

/// <summary>
/// Interaction logic for SearchResult
/// </summary>
public partial class SearchResult
{
    private WindowState _previousWindowState = WindowState.Normal;
    private WindowStyle _previousWindowStyle = WindowStyle.SingleBorderWindow;
    private ResizeMode _previousResizeMode = ResizeMode.CanResize;
    private bool _previousShowTitle = true;
    private bool _previousShowNonClientArea = true;
    private bool _previousTopmost;
    private bool _isFullScreen;
    private System.Windows.Window? _parentWindow;

    public SearchResult()
    {
        InitializeComponent();
        WebBox.FullScreenChanged += (_, isFullScreen) => OnVideoFullScreenChanged(isFullScreen);
    }

    private void OnVideoFullScreenChanged(bool isFullScreen)
    {
        _isFullScreen = isFullScreen;

        UpdateWindowFullScreenState(isFullScreen);
        UpdateLayoutFullScreenState(isFullScreen);
    }

    private void UpdateWindowFullScreenState(bool isFullScreen)
    {
        var window = _parentWindow ??= System.Windows.Window.GetWindow(this);
        if (_parentWindow != null)
        {
            _parentWindow.Activated -= OnWindowActivated;
            _parentWindow.Deactivated -= OnWindowDeactivated;
            _parentWindow.Activated += OnWindowActivated;
            _parentWindow.Deactivated += OnWindowDeactivated;
        }

        if (window is HandyControl.Controls.Window hcWindow)
        {
            ApplyHandyWindowFullScreenState(hcWindow, isFullScreen);
        }
        else if (window != null)
        {
            ApplyStandardWindowFullScreenState(window, isFullScreen);
        }

        if (isFullScreen)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                if (_isFullScreen && _parentWindow != null)
                {
                    _parentWindow.Topmost = true;
                    _parentWindow.Activate();
                    _parentWindow.Focus();
                }
            }), System.Windows.Threading.DispatcherPriority.Input);
        }
    }

    private void ApplyHandyWindowFullScreenState(HandyControl.Controls.Window hcWindow, bool isFullScreen)
    {
        if (isFullScreen)
        {
            _previousWindowState = hcWindow.WindowState;
            _previousWindowStyle = hcWindow.WindowStyle;
            _previousResizeMode = hcWindow.ResizeMode;
            _previousShowTitle = hcWindow.ShowTitle;
            _previousShowNonClientArea = hcWindow.ShowNonClientArea;
            _previousTopmost = hcWindow.Topmost;

            hcWindow.ShowTitle = false;
            hcWindow.ShowNonClientArea = false;
            hcWindow.IsFullScreen = true;
            hcWindow.Topmost = true;
            hcWindow.Activate();
            hcWindow.Focus();
        }
        else
        {
            hcWindow.Topmost = _previousTopmost;
            hcWindow.IsFullScreen = false;
            hcWindow.ShowTitle = _previousShowTitle;
            hcWindow.ShowNonClientArea = _previousShowNonClientArea;
            hcWindow.WindowState = _previousWindowState;
            hcWindow.WindowStyle = _previousWindowStyle;
            hcWindow.ResizeMode = _previousResizeMode;
            hcWindow.Activate();
            hcWindow.Focus();
        }
    }

    private void ApplyStandardWindowFullScreenState(System.Windows.Window window, bool isFullScreen)
    {
        if (isFullScreen)
        {
            _previousWindowState = window.WindowState;
            _previousWindowStyle = window.WindowStyle;
            _previousResizeMode = window.ResizeMode;
            _previousTopmost = window.Topmost;

            window.WindowStyle = WindowStyle.None;
            window.ResizeMode = ResizeMode.NoResize;
            window.WindowState = WindowState.Normal;
            window.WindowState = WindowState.Maximized;
            window.Topmost = true;
            window.Activate();
            window.Focus();
        }
        else
        {
            window.Topmost = _previousTopmost;
            window.WindowState = _previousWindowState;
            window.WindowStyle = _previousWindowStyle;
            window.ResizeMode = _previousResizeMode;
            window.Activate();
            window.Focus();
        }
    }

    private void UpdateLayoutFullScreenState(bool isFullScreen)
    {
        if (isFullScreen)
        {
            HeaderRow.Height = new GridLength(0);
            BottomActionBarRow.Height = new GridLength(0);
            HeaderToolbar.Visibility = Visibility.Collapsed;
            BottomActionBar.Visibility = Visibility.Collapsed;

            CandidatesColumn.Width = new GridLength(0);
            DetailsColumn.Width = new GridLength(1, GridUnitType.Star);
            CandidatesPanel.Visibility = Visibility.Collapsed;

            PosterMetaRow.Height = new GridLength(0);
            FolderFilesRow.Height = new GridLength(0);
            PosterMetaSection.Visibility = Visibility.Collapsed;
            FolderFilesSection.Visibility = Visibility.Collapsed;

            RootLayoutGrid.Margin = new Thickness(0);
            DetailsPanel.Padding = new Thickness(0);
            DetailsPanel.BorderThickness = new Thickness(0);
            DetailsPanel.CornerRadius = new CornerRadius(0);
            DetailsPanel.Background = Brushes.Black;
            VideoContainer.BorderThickness = new Thickness(0);
            VideoContainer.CornerRadius = new CornerRadius(0);
            VideoContainer.Background = Brushes.Black;
            Background = Brushes.Black;
        }
        else
        {
            HeaderRow.Height = GridLength.Auto;
            BottomActionBarRow.Height = GridLength.Auto;
            HeaderToolbar.Visibility = Visibility.Visible;
            BottomActionBar.Visibility = Visibility.Visible;

            CandidatesColumn.Width = new GridLength(4.2, GridUnitType.Star);
            DetailsColumn.Width = new GridLength(5.8, GridUnitType.Star);
            CandidatesPanel.Visibility = Visibility.Visible;

            PosterMetaRow.Height = GridLength.Auto;
            FolderFilesRow.Height = GridLength.Auto;
            PosterMetaSection.Visibility = Visibility.Visible;
            FolderFilesSection.Visibility = Visibility.Visible;

            RootLayoutGrid.Margin = new Thickness(16, 12, 16, 12);
            DetailsPanel.Padding = new Thickness(14);
            DetailsPanel.BorderThickness = new Thickness(1);
            DetailsPanel.CornerRadius = new CornerRadius(8);
            DetailsPanel.Background = (Brush)FindResource("SecondaryRegionBrush");
            VideoContainer.BorderThickness = new Thickness(1);
            VideoContainer.CornerRadius = new CornerRadius(6, 6, 0, 0);
            VideoContainer.Background = Brushes.Black;
            Background = (Brush)FindResource("RegionBrush");
        }
    }

    private void OnWindowActivated(object? sender, EventArgs e)
    {
        if (_isFullScreen && _parentWindow != null)
        {
            _parentWindow.Topmost = true;
        }
    }

    private void OnWindowDeactivated(object? sender, EventArgs e)
    {
        if (_isFullScreen && _parentWindow != null)
        {
            _parentWindow.Topmost = false;
        }
    }

    private void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape && _isFullScreen)
        {
            WebBox.ExitFullScreen();
            e.Handled = true;
        }
    }

    private void UserControl_Unloaded(object sender, RoutedEventArgs e)
    {
        if (_parentWindow != null)
        {
            _parentWindow.Activated -= OnWindowActivated;
            _parentWindow.Deactivated -= OnWindowDeactivated;
        }

        if (_isFullScreen)
        {
            var window = _parentWindow ?? System.Windows.Window.GetWindow(this);
            if (window is HandyControl.Controls.Window hcWindow)
            {
                hcWindow.Topmost = _previousTopmost;
                hcWindow.IsFullScreen = false;
                hcWindow.ShowTitle = _previousShowTitle;
                hcWindow.ShowNonClientArea = _previousShowNonClientArea;
                hcWindow.WindowState = _previousWindowState;
                hcWindow.WindowStyle = _previousWindowStyle;
                hcWindow.ResizeMode = _previousResizeMode;
            }
            else if (window != null)
            {
                window.Topmost = _previousTopmost;
                window.WindowState = _previousWindowState;
                window.WindowStyle = _previousWindowStyle;
                window.ResizeMode = _previousResizeMode;
            }
        }
        WebBox.DisposeBrowser();
    }
}