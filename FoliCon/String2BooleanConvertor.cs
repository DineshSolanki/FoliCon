using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
public class String2BooleanConvertor :ResourceDictionary, IValueConverter 
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return parameter?.Equals(value);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value != null && (bool) value ? parameter : DependencyProperty.UnsetValue;
        }
    }