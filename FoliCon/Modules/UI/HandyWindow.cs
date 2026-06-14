using Brush = System.Windows.Media.Brush;
using Window = HandyControl.Controls.Window;

namespace FoliCon.Modules.UI;

[Localizable(false)]
public class HandyWindow : Window, IDialogWindow
{
    static HandyWindow()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(HandyWindow),
            new FrameworkPropertyMetadata(typeof(Window)));
    }

    public HandyWindow()
    {
        ShowTitle = true;
        HandleDataContextChanged();
        DataContextChanged += HandyWindow_DataContextChanged;
        Background = (Brush)FindResource("RegionBrush");
    }

    public IDialogResult Result { get; set; }

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        HandleDataContextChanged();
    }
    
    private void HandyWindow_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if(e.OldValue is IDialogAware and INotifyPropertyChanged oldNotifyPropertyChanged)
        {
            oldNotifyPropertyChanged.PropertyChanged -= DialogAwareOnPropertyChanged;
        }
        HandleDataContextChanged();
    }

    private void HandleDataContextChanged()
    {
        if (DataContext is not IDialogAware)
        {
            return;
        }

        // Set title and subscribe to updates. Prism 9 removed IDialogAware.Title,
        // so the title is read from the ViewModel's own Title property.
        UpdateTitle(DataContext);
        if(DataContext is INotifyPropertyChanged notifyPropertyChanged)
        {
            notifyPropertyChanged.PropertyChanged += DialogAwareOnPropertyChanged;
        }
    }

    private void DialogAwareOnPropertyChanged(object sender, PropertyChangedEventArgs args)
    {
        // Only update when the 'Title' property changes
        if(args.PropertyName == "Title")
        {
            UpdateTitle(sender);
        }
    }

    private void UpdateTitle(object dataContext)
    {
        if (dataContext?.GetType().GetProperty("Title")?.GetValue(dataContext) is string title)
        {
            Title = title;
        }
    }
}