using ExtensionMethods;
using System;
using System.Collections.Generic;
using System.IO;

namespace Utils
{
    static class Files
    {

        static char[] InvalidFileChars;
        static char[] InvalidDirChars;

        static Files()
        {
            InvalidFileChars = Path.GetInvalidFileNameChars();
            InvalidDirChars = Path.GetInvalidPathChars();
        }

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
        /// Removes invalid chars from path.
        /// Path can be dir or file but should be absolute.
        /// </summary>
        public static string MakeFilesystemSafe(string path)
        {
            path = path.Replace('\\', Path.DirectorySeparatorChar).Replace('/', Path.DirectorySeparatorChar); ;

            string drive = Path.GetPathRoot(path);
            char[] pchars = string.Join(";", path.Split(':')).ToCharArray();
            for (int i = 0; i < drive.Length; i++)
            {
                pchars[i] = drive[i];
            }
            path = pchars.Decode();

            string file = path.Split(Path.DirectorySeparatorChar).Last();
            file = string.Join("", file.Split(InvalidFileChars));

            string dir = Path.GetDirectoryName(path).Substring(drive.Length);

            dir = string.Join("", dir.Split(InvalidDirChars));
            return Path.Combine(drive, dir, file);
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
            catch (Exception _) { }
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
