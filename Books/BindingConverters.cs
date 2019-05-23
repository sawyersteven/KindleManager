using System;
using System.Windows.Data;
using System.Collections.ObjectModel;
using System.Linq;

namespace Books.BindingConverters
{
    /// <summary>
    /// Sets Collapsed
    /// </summary>
    public class CollapseIfNull : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return value == null ? "Collapsed" : "Visible";
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class CollapseIfFalse : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return (bool)value ? "Visible" : "Collapsed";
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class BookInLibrary : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            var id = ((Database.BookEntry)values[0]).Id;
            return ((ObservableCollection<Database.BookEntry>)values[1]).Any(x => x.Id == id) ? "✓" : "";
        }

        public object[] ConvertBack(object value, Type[] targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
