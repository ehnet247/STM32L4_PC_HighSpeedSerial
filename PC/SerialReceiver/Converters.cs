using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Data;

namespace SerialReceiver.Converters;

public class BoolToOpenCloseConverterConverters : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
        if (value is bool isClosed)
        {
            return isClosed ? "Open" : "Close";
        }
        throw new ArgumentException("Value must be a boolean");
    }

    public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
