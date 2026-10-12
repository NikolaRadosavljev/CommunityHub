using System;
using System.Globalization;
using System.Windows.Data;

namespace CommunityHub.Ui.Views.TenantViews
{
    public class DateToEnglishConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is DateTime date)
            {
                return date.ToString("dddd, dd/MM/yyyy", CultureInfo.CreateSpecificCulture("en-US"));
            }
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value;
        }
    }
}
