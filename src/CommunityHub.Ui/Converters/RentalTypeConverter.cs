using System;
using System.Globalization;
using System.Windows.Data;

namespace CommunityHub.Ui.Converters;

public class RentalTypeConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string rentalType)
        {
            return rentalType switch
            {
                "SingleDay" => "1 day",
                "MultipleDays" => "multiple days",
                _ => rentalType
            };
        }
        return "Unknown";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}