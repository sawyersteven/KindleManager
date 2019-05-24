using System;
using System.Windows.Data;
using System.Collections.ObjectModel;
using System.Linq;
using System.Collections.Generic;

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

    /// <summary>
    /// Returns unicode checkmark if Book ID is in IEnumerable<Databse.BookEntry>
    /// 
    /// values[0] : Database.BookEntry
    /// values[1] : IEnumerable<Database.BookEntry>
    /// </summary>
    public class BookInLibrary : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            var id = ((Database.BookEntry)values[0]).Id;
            return ((IEnumerable<Database.BookEntry>)values[1]).Any(x => x.Id == id);// ? "✓" : "";
        }

        public object[] ConvertBack(object value, Type[] targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Merges two IEnumerable<Database.BookEntry> byt including objects in the
    ///   second IEnumerable only if their ID prop is not in the first IEnumerable
    /// </summary>
    public class MergeLibraries : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            var local = (IEnumerable<Database.BookEntry>)values[0];
            var remote = (IEnumerable<Database.BookEntry>)values[1];
            var l = local.ToList();
            l.AddRange(remote.Where(x => !local.Any(y => y.Id == x.Id)));
            return l;
        }

        public object[] ConvertBack(object value, Type[] targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
