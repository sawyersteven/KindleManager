using ExtensionMethods;
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

        /// <summary>
        /// Removes empty dirs starting at 'start' moving upward to 'stop'.
        /// Stops are first directory that is not empty or when something goes wrong.
        /// </summary>
        /// <param name="dir"></param>
        public static void CleanBackward(string start, string stop)
        {
            start = start.NormPath();
            stop = stop.NormPath();
            try
            {
                while (Directory.GetFiles(start).Length == 0)
                {
                    if (start == stop) break;
                    try
                    {
                        Directory.Delete(start);
                    }
                    catch (DirectoryNotFoundException _) { }
                    start = Path.GetFullPath(Path.Combine(start, ".."));
                }
            }
            catch (Exception _) { }
        }
    }
}
