﻿using Brush = System.Windows.Media.Brush;
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
        if (DataContext is not IDialogAware dialogAware)
        {
            return;
        }

        // Set title and subscribe to updates
        Title = dialogAware.Title;
        if(dialogAware is INotifyPropertyChanged notifyPropertyChanged)
        {
            notifyPropertyChanged.PropertyChanged += DialogAwareOnPropertyChanged;
        }
    }

    private void DialogAwareOnPropertyChanged(object sender, PropertyChangedEventArgs args)
    {
        // Only update when the 'Title' property changes
        if(args.PropertyName == nameof(IDialogAware.Title) && sender is IDialogAware dialogAware)
        {
            Title = dialogAware.Title;
        }
    }
}