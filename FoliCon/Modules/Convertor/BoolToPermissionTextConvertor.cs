﻿namespace FoliCon.Modules.Convertor;

public class BoolToPermissionTextConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool hasWritePermission)
        {
            return hasWritePermission ? LangProvider.GetLang("WritePermissionAllowed") : LangProvider.GetLang("WritePermissionNotAllowed");
        }

        throw new InvalidOperationException("Must be a boolean value.");
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}