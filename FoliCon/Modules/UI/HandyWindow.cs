namespace FoliCon.Modules.UI;

public class HandyWindow : HandyControl.Controls.Window, IDialogWindow
{
    static HandyWindow()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(HandyWindow),
            new FrameworkPropertyMetadata(typeof(HandyControl.Controls.Window)));
    }

    public HandyWindow()
    {
        ShowTitle = true;
        InitializeProperties();
        Background = (System.Windows.Media.Brush)FindResource("RegionBrush");
    }

    public IDialogResult Result { get; set; }

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        InitializeProperties();
    }

    private void InitializeProperties()
    {
        if (DataContext is IDialogAware dialogAware)
        {
            Title = dialogAware.Title;
        }
    }
}