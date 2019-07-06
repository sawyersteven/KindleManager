using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Data;

namespace KindleManager.BindingConverters
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

    public class VisibilityToBool : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            switch ((Visibility)value)
            {
                case Visibility.Collapsed:
                    return false;
                case Visibility.Hidden:
                    return false;
                case Visibility.Visible:
                    return true;
            }
            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (bool)value ? Visibility.Visible : Visibility.Collapsed;
        }
    }


    public class GridColumnFilter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var v = (IEnumerable<System.Windows.Controls.DataGridColumn>)value;

            if (v == null) return value;
            List<System.Windows.Controls.DataGridColumn> output = new List<System.Windows.Controls.DataGridColumn>();

            foreach (var col in v)
            {
                if (col.Header != null)
                {
                    output.Add(col);
                }
            }
            return output;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
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
            KindleManager.Database.BookEntry book = values[0] as KindleManager.Database.BookEntry;
            return book == null ? false : ((IEnumerable<KindleManager.Database.BookEntry>)values[1]).Any(x => x.Id == book.Id);
        }

        public object[] ConvertBack(object value, Type[] targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Tests if metatada for id matches in both databases
    /// 
    /// values[0] : int
    /// values[1] : Database
    /// values[2] : Database
    /// </summary>
    public class MetadataMatch : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            int id;
            try
            {
                id = (int)values[0];
            }
            catch (Exception _) { return false; }

            ObservableCollection<Database.BookEntry> db1 = values[1] as ObservableCollection<Database.BookEntry>;
            ObservableCollection<Database.BookEntry> db2 = values[2] as ObservableCollection<Database.BookEntry>;
            if (db1 == null || db2 == null) { return true; }

            Database.BookEntry bk1 = db1.FirstOrDefault(x => x.Id == id);
            Database.BookEntry bk2 = db2.FirstOrDefault(x => x.Id == id);
            if (bk1 == null || bk2 == null) { return true; }

            Dictionary<string, string> props1 = bk1.Props();
            foreach (KeyValuePair<string, string> kv in bk2.Props())
            {
                string v1 = props1[kv.Key];
                string v2 = kv.Value;
                if ((props1[kv.Key] ?? "") != (kv.Value ?? ""))
                {
                    return false;
                }
            }
            return true;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Merges two IEnumerable<Database.BookEntry> by including objects in the
    ///   second IEnumerable only if their ID prop is not in the first IEnumerable
    /// </summary>
    public class MergeLibraries : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {

            var local = values[0] as IEnumerable<KindleManager.Database.BookEntry>;
            var remote = values[1] as IEnumerable<KindleManager.Database.BookEntry>;
            if (local == null || remote == null) return local;
            var l = local.ToList();
            l.AddRange(remote.Where(x => !local.Any(y => y.Id == x.Id)));
            return l;
        }

        public object[] ConvertBack(object value, Type[] targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class FloatToString : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (float)value == 0 ? "" : value.ToString();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
