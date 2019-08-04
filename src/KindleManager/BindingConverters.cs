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

    public class StrToFloat : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return null;
            float v = (float)value;
            if (v == 0)
            {
                return null;
            }
            return ((float)value).ToString("F1");
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string v = (string)value;
            if (v == "")
            {
                return null;
            }
            else if (float.TryParse(v, out float f))
            {
                return f;
            }
            else
            {
                return null;
            }
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
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            LibraryEntry book = values[0] as LibraryEntry;
            if (book == null) return false;

            var library = values[1] as IEnumerable<LibraryEntry>;

            if ((string)parameter == "local")
            {
                return library.Any(x => x.IsLocal && x.Id == book.Id);
            }
            else
            {
                return library.Any(x => x.IsRemote && x.Id == book.Id);
            }
        }

        public object[] ConvertBack(object value, Type[] targetType, object parameter, CultureInfo culture)
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
            if (value == null) return "";
            return (float)value == 0 ? "" : value.ToString();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
