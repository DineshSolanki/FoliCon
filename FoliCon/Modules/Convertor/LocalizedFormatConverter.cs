namespace FoliCon.Modules.Convertor;

/// <summary>
/// Formats a localized format string against one or more bound values.
///
/// XAML's <c>StringFormat</c> is a plain CLR property on <c>BindingBase</c>, so it cannot itself
/// be bound to a resource — the format text would have to be hardcoded in the markup. A
/// MultiBinding whose first value is the format string (bound to <c>LangProvider</c>) routes
/// around that, and re-evaluates on a live language switch like every other localized binding.
/// </summary>
/// <example>
/// <code>
/// &lt;TextBlock&gt;
///   &lt;TextBlock.Text&gt;
///     &lt;MultiBinding Converter="{StaticResource LocalizedFormat}"&gt;
///       &lt;Binding Source="{StaticResource FoliConLangs}" Path="OverlayRemoveAutomation" /&gt;
///       &lt;Binding Path="DisplayName" /&gt;
///     &lt;/MultiBinding&gt;
///   &lt;/TextBlock.Text&gt;
/// &lt;/TextBlock&gt;
/// </code>
/// </example>
[Localizable(false)]
public class LocalizedFormatConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values is not { Length: > 0 } || values[0] is not string format)
        {
            return string.Empty;
        }

        var args = new object[values.Length - 1];
        for (var i = 1; i < values.Length; i++)
        {
            // DependencyProperty.UnsetValue reaches here while a MultiBinding is still
            // resolving; formatting it would print the sentinel's type name.
            args[i - 1] = values[i] == DependencyProperty.UnsetValue ? string.Empty : values[i];
        }

        try
        {
            return string.Format(culture, format, args);
        }
        catch (FormatException)
        {
            // A translation with a malformed placeholder must not take the window down;
            // showing the untranslated pattern is the least destructive fallback.
            return format;
        }
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
