using System;
using System.IO;
using System.Reflection;

namespace XML_Editor_WuffPad.Core
{
    /// <summary>
    /// Resolves the directory WuffPad keeps its working data in.
    /// </summary>
    /// <remarks>
    /// WuffPad used to download this data from its project server at every start.
    /// The server is gone, so the files now ship inside the executable (see
    /// <see cref="BuiltinResources"/>) and get written to disk on first start.
    /// The old location (a "Resources" folder next to the executable) is kept, so
    /// everyone who still has a populated folder simply keeps using it.
    /// </remarks>
    public static class AppData
    {
        /// <summary>Name of the folder next to the executable that holds the data.</summary>
        public const string FolderName = "Resources";

        private static string resolvedDirectory;
        private static bool? executableDirectoryIsWritable;

        /// <summary>
        /// The directory holding language.xml, descriptions.dict, standardKeys.db
        /// and emojis.txt.
        /// </summary>
        public static string Directory
        {
            get
            {
                if (resolvedDirectory == null)
                {
                    string besideExecutable = Path.Combine(ExecutableDirectory, FolderName);
                    resolvedDirectory = ExecutableDirectoryIsWritable
                        ? besideExecutable
                        : Path.Combine(Environment.GetFolderPath(
                            Environment.SpecialFolder.LocalApplicationData), "WuffPad", FolderName);
                }
                return resolvedDirectory;
            }
        }

        /// <summary>Directory the executable lives in.</summary>
        public static string ExecutableDirectory
        {
            get
            {
                string codeBase = Assembly.GetExecutingAssembly().CodeBase;
                UriBuilder uri = new UriBuilder(codeBase);
                string path = Uri.UnescapeDataString(uri.Path);
                return Path.GetDirectoryName(path);
            }
        }

        /// <summary>Full path of a data file inside <see cref="Directory"/>.</summary>
        public static string PathFor(string fileName)
        {
            return Path.Combine(Directory, fileName);
        }

        /// <summary>
        /// True when the application may write next to its executable. Installs in
        /// Program Files (or any read-only medium) fall back to LocalApplicationData.
        /// </summary>
        public static bool ExecutableDirectoryIsWritable
        {
            get
            {
                if (!executableDirectoryIsWritable.HasValue)
                {
                    executableDirectoryIsWritable = CanWriteTo(ExecutableDirectory);
                }
                return executableDirectoryIsWritable.Value;
            }
        }

        /// <summary>Creates <see cref="Directory"/> if it does not exist yet.</summary>
        public static void EnsureDirectory()
        {
            if (!System.IO.Directory.Exists(Directory))
            {
                System.IO.Directory.CreateDirectory(Directory);
            }
        }

        private static bool CanWriteTo(string path)
        {
            try
            {
                string probe = Path.Combine(path, ".wuffpad-write-test");
                using (FileStream fs = File.Create(probe, 1, FileOptions.DeleteOnClose))
                {
                    fs.WriteByte(0);
                }
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
