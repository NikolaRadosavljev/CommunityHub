using System;
using System.Globalization;
using System.Windows.Data;

namespace CommunityHub.Ui.Views.TenantViews
{
    public class StatusConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string status)
            {
                return status switch
                {
                    "scheduled" => "Scheduled",
                    "confirmed" => "Confirmed",
                    "cancelled" => "Cancelled",
                    _ => status
                };
            }
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string status)
            {
                return status switch
                {
                    "Scheduled" => "scheduled",
                    "Confirmed" => "confirmed",
                    "Cancelled" => "cancelled",
                    _ => status
                };
            }
            return value;
        }
    }
}
