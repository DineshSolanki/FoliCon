namespace FoliCon.Modules.UI;

public class ClickBehavior : Behavior<System.Windows.Controls.Image>
{
    private readonly DispatcherTimer _timer = new();

    public ClickBehavior()
    {
        _timer.Interval = TimeSpan.FromSeconds(0.2);
        _timer.Tick += Timer_Tick;
    }

    public static readonly DependencyProperty ClickCommandProperty =
        DependencyProperty.Register(nameof(ClickCommand), typeof(ICommand), typeof(ClickBehavior));
    public static readonly DependencyProperty CommandParameterProperty =
        DependencyProperty.Register(nameof(CommandParameter), typeof(object), typeof(ClickBehavior));
    public object CommandParameter
    {
        get => GetValue(CommandParameterProperty);
        set => SetValue(CommandParameterProperty, value);
    }
    public ICommand ClickCommand
    {
        get => (ICommand)GetValue(ClickCommandProperty);
        set => SetValue(ClickCommandProperty, value);
    }

    public static readonly DependencyProperty DoubleClickCommandProperty =
        DependencyProperty.Register(nameof(DoubleClickCommand), typeof(ICommand), typeof(ClickBehavior));

    public ICommand DoubleClickCommand
    {
        get => (ICommand)GetValue(DoubleClickCommandProperty);
        set => SetValue(DoubleClickCommandProperty, value);
    }

    protected override void OnAttached()
    {
        base.OnAttached();
        AssociatedObject.Loaded += AssociatedObject_Loaded;
        AssociatedObject.Unloaded += AssociatedObject_Unloaded;
    }

    private void AssociatedObject_Loaded(object sender, RoutedEventArgs e)
    {
        AssociatedObject.Loaded -= AssociatedObject_Loaded;
        AssociatedObject.PreviewMouseLeftButtonDown += AssociatedObject_PreviewMouseLeftButtonDown;
    }

    private void AssociatedObject_Unloaded(object sender, RoutedEventArgs e)
    {
        AssociatedObject.Unloaded -= AssociatedObject_Unloaded;
        AssociatedObject.PreviewMouseLeftButtonDown -= AssociatedObject_PreviewMouseLeftButtonDown;
    }

    private void Timer_Tick(object sender, EventArgs e)
    {
        _timer.Stop();
        ClickCommand?.Execute(CommandParameter);
    }

    private void AssociatedObject_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 2)
        {
            _timer.Stop();
            DoubleClickCommand?.Execute(CommandParameter);
        }
        else
        {
            _timer.Start();
        }
    }
}