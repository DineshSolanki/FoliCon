namespace FoliCon.Modules.Convertor;

[Localizable(false)]
public class BoolToPermissionTextConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool hasWritePermission)
        {
            return hasWritePermission ? Lang.WritePermissionAllowed : Lang.WritePermissionNotAllowed;
        }

        throw new InvalidOperationException("Must be a boolean value.");
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}