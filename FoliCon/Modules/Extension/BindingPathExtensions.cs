using FoliCon.Modules.Convertor;
using Image = System.Windows.Controls.Image;

namespace FoliCon.Modules.Extension;

public static class BindingPathExtensions
{
    private static readonly ImageCacheConverter ImageCacheConverter = new ();
    
    public static readonly DependencyProperty BindingPathProperty =
        DependencyProperty.RegisterAttached(
            "BindingPath",
            typeof(string),
            typeof(BindingPathExtensions),
            new PropertyMetadata("", BindingPathPropertyChanged));

    public static string GetBindingPath(DependencyObject obj)
    {
        return (string)obj.GetValue(BindingPathProperty);
    }

    public static void SetBindingPath(DependencyObject obj, string value)
    {
        obj.SetValue(BindingPathProperty, value);
    }

    private static void BindingPathPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var img = d as Image;
        var bindingPath = (string)e.NewValue;
        var binding = new Binding();
        
        var userControl = FindAncestor<ImageGalleryControl>(img);
        if (userControl is { UseCacheConverter: true })
        {
            binding.Converter = ImageCacheConverter;
        }
        // Only set Binding.Path if it's not direct DataContext binding
        if (bindingPath != ".") 
        {
            binding.Path = new PropertyPath(bindingPath);
        }
        img?.SetBinding(Image.SourceProperty, binding);
    }

    private static T FindAncestor<T>(DependencyObject dependencyObject) where T : class
    {
        var current = dependencyObject;
        do
        {
            if (current is T typed)
            {
                return typed;
            }
            current = VisualTreeHelper.GetParent(current);
        }
        while (current != null);
        return null;
    }
}