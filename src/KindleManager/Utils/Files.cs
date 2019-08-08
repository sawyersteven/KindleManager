using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace Utils
{
    public static class Files
    {
        private static readonly string InvalidRegStr = string.Format(@"([{0}]*\.+$)|([{0}]+)", Regex.Escape(new string(Path.GetInvalidFileNameChars())));

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
                        files.AddRange(Directory.GetFiles(subdir));
                    }
                    files.AddRange(DirSearch(subdir));
                }
            }
            catch { }
            return files.ToArray();
        }

        /// <summary>
        /// Removes invalid chars from path.
        /// Path can be dir or file but should be absolute.
        /// </summary>
        public static string MakeFilesystemSafe(string path)
        {
            string[] parts = path.Split(Path.DirectorySeparatorChar);

            for (int i = 1; i < parts.Length; i++)
            {
                parts[i] = Regex.Replace(parts[i], InvalidRegStr, "_");
                while (parts[i].Contains("__"))
                {
                    parts[i] = parts[i].Replace("__", "_");
                }
            }

            return string.Join(Path.DirectorySeparatorChar.ToString(), parts);
        }

        /// <summary>
        /// Removes empty dirs starting at 'start' moving upward to 'stop'.
        /// Stops are first directory that is not empty or when something goes wrong.
        /// </summary>
        public static void CleanBackward(string start, string stop)
        {
            start = Path.GetFullPath(start);
            stop = Path.GetFullPath(stop);
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
            catch (Exception) { }
        }

        /// <summary>
        /// Removes every child of 'current' that contains no files.
        /// Will remove nested empty directories.
        /// </summary>
        public static void CleanForward(string current)
        {
            if (File.Exists(current)) return;

            string[] childDirs = Directory.GetDirectories(current);
            foreach (string child in childDirs)
            {
                CleanForward(child);
            }

            if (Directory.GetFileSystemEntries(current).Length == 0)
            {
                Directory.Delete(current);
                return;
            }
        }
    }
}
