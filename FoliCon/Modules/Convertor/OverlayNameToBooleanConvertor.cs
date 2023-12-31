namespace FoliCon.Modules.Convertor;

public class OverlayNameToBooleanConvertor : IMultiValueConverter
{

    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        return values.Length > 1 
               && values[0] != null 
               && values[1] != null 
               && values[0].Equals(values[1]);
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        if (value != null && (bool)value)
        {
            return [null, (string)parameter];
        }

        return null;
    }
}