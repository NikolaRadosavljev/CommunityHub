using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Windows.Data;

namespace CommunityHub.Ui.Converters
{
    public class FlatsRangeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string range)
            {
                var parts = range.Split('-');
                if (parts.Length == 2 && int.TryParse(parts[0], out int min) && int.TryParse(parts[1], out int max))
                    return new ObservableCollection<int>(Enumerable.Range(min, max - min + 1));
                if (parts.Length == 1 && int.TryParse(parts[0], out int single))
                    return new ObservableCollection<int> { single };
            }
            return new ObservableCollection<int>();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}