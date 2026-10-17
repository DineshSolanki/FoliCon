#nullable enable
namespace FoliCon.Modules.Convertor;

/// <summary>
/// Formats a localized format string against one or more bound values.
///
/// XAML's <c>StringFormat</c> is a plain CLR property on <c>BindingBase</c>, so it cannot itself
/// be bound to a resource — the format text would have to be hardcoded in the markup. A
/// MultiBinding whose first value is the format string (bound to <c>LangProvider</c>) routes
/// around that, and re-evaluates on a live language switch like every other localized binding.
///
/// Supports singular and plural forms separated by '|' (singular|plural).
/// The governing count is determined by the last numeric argument, falling back to the first numeric argument.
/// If count == 1, the singular form is chosen; otherwise the plural form is used.
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

        var effectiveFormat = format;
        if (format.Contains('|'))
        {
            var parts = format.Split('|');
            if (parts.Length == 2)
            {
                var isSingular = TryDetermineCount(args, out var count) && count == 1;
                effectiveFormat = isSingular ? parts[0] : parts[1];
            }
        }

        try
        {
            return string.Format(culture, effectiveFormat, args);
        }
        catch (FormatException)
        {
            // A translation with a malformed placeholder must not take the window down;
            // showing the untranslated pattern is the least destructive fallback.
            return effectiveFormat;
        }
    }

    private static bool TryDetermineCount(object[] args, out long count)
    {
        if (args.Length > 0 && TryGetCount(args[^1], out count))
        {
            return true;
        }

        foreach (var t in args)
        {
            if (TryGetCount(t, out count))
            {
                return true;
            }
        }

        count = 0;
        return false;
    }

    private static bool TryGetCount(object? value, out long count)
    {
        switch (value)
        {
            case sbyte sb: count = sb; return true;
            case byte b: count = b; return true;
            case short s: count = s; return true;
            case ushort us: count = us; return true;
            case int i: count = i; return true;
            case uint ui: count = ui; return true;
            case long l: count = l; return true;
            case ulong ul: count = ul > (ulong)long.MaxValue ? long.MaxValue : (long)ul; return true;
            case float f: count = (long)f; return true;
            case double d: count = (long)d; return true;
            case decimal m: count = (long)m; return true;
            case string str when long.TryParse(str, out var parsed):
                count = parsed;
                return true;
            default:
                count = 0;
                return false;
        }
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
