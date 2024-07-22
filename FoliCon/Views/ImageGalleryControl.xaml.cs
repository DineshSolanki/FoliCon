using System.Collections;
using Image = System.Windows.Controls.Image;

namespace FoliCon.Views;

public partial class ImageGalleryControl : UserControl
{
    public ImageGalleryControl()
    {
        InitializeComponent();
    }

    #region Variables

    private bool _autoScroll = true;
    
    public static readonly DependencyProperty CustomBusyContentTemplateProperty =
        DependencyProperty.Register(nameof(CustomBusyContentTemplate), typeof(DataTemplate), typeof(ImageGalleryControl), new PropertyMetadata(null));

    public static readonly DependencyProperty ItemsSourceProperty =
        DependencyProperty.Register(nameof(ItemsSource), typeof(IEnumerable), typeof(ImageGalleryControl), new PropertyMetadata(null));

    public static readonly DependencyProperty DoubleClickCommandProperty =
        DependencyProperty.Register(nameof(DoubleClickCommand), typeof(ICommand), typeof(ImageGalleryControl), new PropertyMetadata(null));

    public static readonly DependencyProperty ClickCommandProperty =
        DependencyProperty.Register(nameof(ClickCommand), typeof(ICommand), typeof(ImageGalleryControl), new PropertyMetadata(null));

    public static readonly DependencyProperty BindingPathProperty = 
        DependencyProperty.Register(
            "BindingPath", 
            typeof(string), 
            typeof(ImageGalleryControl), 
            new PropertyMetadata("Url")  // Default binding path
        );

    #endregion

    #region Properties

    public DataTemplate CustomBusyContentTemplate
    {
        get => (DataTemplate)GetValue(CustomBusyContentTemplateProperty);
        set => SetValue(CustomBusyContentTemplateProperty, value);
    }
    
    public IEnumerable ItemsSource
    {
        get => (IEnumerable)GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    public ICommand DoubleClickCommand
    {
        get => (ICommand)GetValue(DoubleClickCommandProperty);
        set => SetValue(DoubleClickCommandProperty, value);
    }

    public ICommand ClickCommand
    {
        get => (ICommand)GetValue(ClickCommandProperty);
        set => SetValue(ClickCommandProperty, value);
    }

    public string BindingPath
    {
        get => (string) GetValue(BindingPathProperty);
        set => SetValue(BindingPathProperty, value);
    }
    #endregion

    #region Methods

    private void ScrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
    {

        if (e.ExtentHeightChange == 0)
        {
            _autoScroll = ScrollViewer.VerticalOffset == ScrollViewer.ScrollableHeight;
        }


        if (_autoScroll && e.ExtentHeightChange != 0)
        {
            ScrollViewer.ScrollToVerticalOffset(ScrollViewer.ExtentHeight);
        }
    }
    
    private void FrameworkElement_OnUnloaded(object sender, RoutedEventArgs e)
    {
        var image = (Image)sender;
        image.Source = null;
    }

    #endregion
    
}