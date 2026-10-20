using System;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using HandyControl.Tools;
using Prism.Dialogs;
using Brush = System.Windows.Media.Brush;
using Brushes = System.Windows.Media.Brushes;
using Color = System.Windows.Media.Color;
using FontFamily = System.Windows.Media.FontFamily;
using Window = HandyControl.Controls.Window;

namespace FoliCon.Modules.UI;

[Localizable(false)]
public class HandyWindow : Window, IDialogWindow
{
    static HandyWindow()
    {
        var defaultStyle = GetHandyDefaultStyle();
        StyleProperty.OverrideMetadata(typeof(HandyWindow),
            new FrameworkPropertyMetadata(defaultStyle, (PropertyChangedCallback)null, CoerceStyle));
    }

    private static Style GetHandyDefaultStyle() => StyleProperty.GetMetadata(typeof(Window))?.DefaultValue as Style;

    private static object CoerceStyle(DependencyObject d, object baseValue)
    {
        if (baseValue is not Style incomingStyle)
        {
            return baseValue ?? GetHandyDefaultStyle();
        }

        var hasTemplate = incomingStyle.Setters.OfType<Setter>().Any(setter => setter.Property == TemplateProperty);

        if (hasTemplate)
        {
            return baseValue ?? GetHandyDefaultStyle();
        }

        {
            // Extract and apply all property setters directly to the window
            // so Prism's Dialog.WindowStyle configures the window without
            // wiping out HandyControl's Window template or backdrop.
            foreach (var setter in incomingStyle.Setters.OfType<Setter>())
            {
                if (setter.Property == null || setter.Value == null)
                {
                    continue;
                }

                if (setter.Value is not BindingBase binding)
                {
                    if (setter.Value is DynamicResourceExtension dynamicResource)
                    {
                        ((FrameworkElement)d).SetResourceReference(setter.Property, dynamicResource.ResourceKey);
                    }
                    else
                    {
                        d.SetValue(setter.Property, setter.Value);
                    }
                }
                else
                {
                    BindingOperations.SetBinding(d, setter.Property, binding);
                }
            }

            return GetHandyDefaultStyle();
        }

    }

    public HandyWindow()
    {
        SystemBackdropType = BackdropType.Mica;
        ShowTitle = true;
        ShowIcon = true;
        NonClientAreaHeight = 42;
        ApplyThemedBackdropAndBrushes();

        FontFamily = new FontFamily("Segoe UI, Segoe UI Variable, Microsoft YaHei, Arial");
        TextOptions.SetTextFormattingMode(this, TextFormattingMode.Display);
        RenderOptions.SetClearTypeHint(this, ClearTypeHint.Enabled);

        try
        {
            if (Icon == null && Application.Current?.MainWindow?.Icon != null)
            {
                Icon = Application.Current.MainWindow.Icon;
            }
        }
        catch
        {
            // ignored
        }

        HandleDataContextChanged();
        DataContextChanged += HandyWindow_DataContextChanged;
        Loaded += HandyWindow_Loaded;
        WindowBackdropHelper.SuppressNativeCaptionButtons(this);
        WindowBackdropHelper.UpdateImmersiveDarkMode(this, MainWindowViewModel.IsCurrentThemeDark());
    }

    public IDialogResult Result { get; set; }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        ApplyThemedBackdropAndBrushes();
        WindowBackdropHelper.SuppressNativeCaptionButtons(this);
        WindowBackdropHelper.UpdateImmersiveDarkMode(this, MainWindowViewModel.IsCurrentThemeDark());
    }

    private void HandyWindow_Loaded(object sender, RoutedEventArgs e)
    {
        ApplyThemedBackdropAndBrushes();
        WindowBackdropHelper.SuppressNativeCaptionButtons(this);
        WindowBackdropHelper.UpdateImmersiveDarkMode(this, MainWindowViewModel.IsCurrentThemeDark());
    }

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        ApplyThemedBackdropAndBrushes();
        HandleDataContextChanged();
    }

    protected override void OnContentChanged(object oldContent, object newContent)
    {
        base.OnContentChanged(oldContent, newContent);
        // Ensure child UserControl/Content doesn't paint an opaque background over the window backdrop
        if (newContent is Control control)
        {
            control.Background = Brushes.Transparent;
            control.Loaded += (_, _) => control.Background = Brushes.Transparent;
        }
    }

    private void ApplyThemedBackdropAndBrushes()
    {
        SetResourceReference(BackgroundProperty, "WindowBackdropBrush");
        NonClientAreaBackground = Brushes.Transparent;
        SetResourceReference(NonClientAreaForegroundProperty, "PrimaryTextBrush");
        SetResourceReference(OtherButtonHoverBackgroundProperty, "SecondaryRegionBrush");
        SetResourceReference(OtherButtonForegroundProperty, "PrimaryTextBrush");
        CloseButtonHoverBackground = new SolidColorBrush(Color.FromRgb(0xE8, 0x11, 0x23));
    }

    private void HandyWindow_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.OldValue is IDialogAware and INotifyPropertyChanged oldNotifyPropertyChanged)
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
        if (DataContext is INotifyPropertyChanged notifyPropertyChanged)
        {
            notifyPropertyChanged.PropertyChanged += DialogAwareOnPropertyChanged;
        }
    }

    private void DialogAwareOnPropertyChanged(object sender, PropertyChangedEventArgs args)
    {
        // Only update when the 'Title' property changes
        if (args.PropertyName == "Title")
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
