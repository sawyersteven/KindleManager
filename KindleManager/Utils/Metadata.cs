using ExtensionMethods;
using System;
using System.Globalization;

namespace Utils
{
    static class Metadata
    {
        private static readonly Random rndm = new Random();
        private static readonly CultureInfo culture = new CultureInfo("en-US");
        private static readonly string[] dateFormats = new string[] { "yyyy", "yyyy-MM", "yyyy-MM-dd", "MM/dd/yyyy" };

        /// <summary>
        /// Generates random number with default length of 3 digits
        /// </summary>
        public static int RandomNumber(int digits = 3)
        {
            int upper = digits >= 10 ? int.MaxValue : (int)(Math.Pow(10, digits) - 1);
            return rndm.Next(1, upper);
        }

        /// <summary>
        /// Get current timestamp as seconds from Jan 1, 1904, MOBI standard epoch time
        /// </summary>
        public static int TimeStamp()
        {
            TimeSpan t = DateTime.UtcNow - new DateTime(1904, 1, 1);
            return (int)t.TotalSeconds;
        }

        /// <summary>
        /// Get timestamp as seconds from date
        /// </summary>
        public static int TimeStamp(int year, int month, int day)
        {
            TimeSpan t = new DateTime(year, month, day) - new DateTime(1970, 1, 1);
            return (int)t.TotalSeconds;
        }

        /// <summary>
        /// Reorders author name for standard lastname-first sorting ie "Charles Dickens" becomes "Dickens, Charles"
        /// </summary>
        public static string SortAuthor(string author)
        {
            string[] splt = author.Split(' ');
            if (splt.Length == 1) return author;

            return splt[splt.Length - 1] + ", " + string.Join(" ", splt.SubArray(0, splt.Length - 1));
        }

        /// <summary>
        /// Converts date strings into epub standard yyyy-MM-dd (1950-01-01)
        /// </summary>
        public static string GetDate(string date)
        {
            if (date == "")
            {
                return DateTime.UtcNow.ToString("yyyy-MM-dd");
            }
            return DateTime.ParseExact(date.Truncate(10), dateFormats, culture, DateTimeStyles.None).ToString("yyyy-MM-dd");
        }
    }
}
