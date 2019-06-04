using System;
using System.Collections.Generic;
using System.IO;

namespace Utils
{
    static class Files
    {
        /// <summary>
        /// Creates string[] of absolute paths to all files and folders in dir
        /// Ignores dirs that throw any errors (typically access denied)
        /// Pass 'true' for subdirsOnly to get array of subdirs without files
        /// </summary>
        public static string[] DirSearch(string dir, bool subdirsOnly = false)
        {
            List<string> files = new List<string>();
            try
            {
                foreach (string subdir in Directory.GetDirectories(dir))
                {
                    if (!subdirsOnly)
                    {
                        Console.WriteLine(string.Join(", ", Directory.GetFiles(subdir)));
                        files.AddRange(Directory.GetFiles(subdir));
                    }
                    files.AddRange(DirSearch(subdir));
                }
            }
            catch { }
            return files.ToArray();
        }
    }
}
