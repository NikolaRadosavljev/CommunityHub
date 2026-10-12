using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace CommunityHub.Ui.Converters;

public class RentalTypeToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string rentalType && parameter is string paramValue && paramValue == "MultipleDays")
        {
            return rentalType == "MultipleDays" ? Visibility.Visible : Visibility.Collapsed;
        }
        return Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
