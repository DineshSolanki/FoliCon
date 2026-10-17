using System.Windows;
using System.Windows.Input;

namespace FoliCon.Views;

/// <summary>
/// Interaction logic for SearchResult
/// </summary>
public partial class SearchResult
{
    private WindowState _previousWindowState = WindowState.Normal;
    private WindowStyle _previousWindowStyle = WindowStyle.SingleBorderWindow;
    private ResizeMode _previousResizeMode = ResizeMode.CanResize;
    private bool _isFullScreen;

    public SearchResult()
    {
        InitializeComponent();
        WebBox.FullScreenChanged += OnVideoFullScreenChanged;
    }

    private void OnVideoFullScreenChanged(object? sender, bool isFullScreen)
    {
        _isFullScreen = isFullScreen;

        var window = System.Windows.Window.GetWindow(this);
        if (window != null)
        {
            if (isFullScreen)
            {
                _previousWindowState = window.WindowState;
                _previousWindowStyle = window.WindowStyle;
                _previousResizeMode = window.ResizeMode;

                window.WindowStyle = WindowStyle.None;
                window.ResizeMode = ResizeMode.NoResize;
                window.WindowState = WindowState.Normal;
                window.WindowState = WindowState.Maximized;
            }
            else
            {
                window.WindowState = _previousWindowState;
                window.WindowStyle = _previousWindowStyle;
                window.ResizeMode = _previousResizeMode;
            }
        }

        if (isFullScreen)
        {
            HeaderToolbar.Visibility = Visibility.Collapsed;
            BottomActionBar.Visibility = Visibility.Collapsed;
            CandidatesPanel.Visibility = Visibility.Collapsed;
            PosterMetaSection.Visibility = Visibility.Collapsed;
            FolderFilesSection.Visibility = Visibility.Collapsed;

            RootLayoutGrid.Margin = new Thickness(0);
            DetailsPanel.Padding = new Thickness(0);
            DetailsPanel.BorderThickness = new Thickness(0);
            DetailsPanel.CornerRadius = new CornerRadius(0);
            VideoContainer.BorderThickness = new Thickness(0);
            VideoContainer.CornerRadius = new CornerRadius(0);
        }
        else
        {
            HeaderToolbar.Visibility = Visibility.Visible;
            BottomActionBar.Visibility = Visibility.Visible;
            CandidatesPanel.Visibility = Visibility.Visible;
            PosterMetaSection.Visibility = Visibility.Visible;
            FolderFilesSection.Visibility = Visibility.Visible;

            RootLayoutGrid.Margin = new Thickness(16, 12, 16, 12);
            DetailsPanel.Padding = new Thickness(14);
            DetailsPanel.BorderThickness = new Thickness(1);
            DetailsPanel.CornerRadius = new CornerRadius(8);
            VideoContainer.BorderThickness = new Thickness(1);
            VideoContainer.CornerRadius = new CornerRadius(6, 6, 0, 0);
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
        if (_isFullScreen)
        {
            var window = System.Windows.Window.GetWindow(this);
            if (window != null)
            {
                window.WindowState = _previousWindowState;
                window.WindowStyle = _previousWindowStyle;
                window.ResizeMode = _previousResizeMode;
            }
        }
        WebBox.DisposeBrowser();
    }
}