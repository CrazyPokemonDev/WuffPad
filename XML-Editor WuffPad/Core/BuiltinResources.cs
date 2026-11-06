using System.IO;
using System.Reflection;

namespace XML_Editor_WuffPad.Core
{
    /// <summary>
    /// The helper files WuffPad used to download from crazypokemondev.de.
    /// They are embedded in the executable and extracted on demand.
    /// </summary>
    /// <remarks>
    /// Rebuild the files with tools/make-builtin-resources.py from the upstream
    /// Werewolf langfiles (GreyWolfDev/Werewolf, GPL-3.0).
    /// </remarks>
    public static class BuiltinResources
    {
        public const string LanguageFile = "language.xml";
        public const string DescriptionsFile = "descriptions.dict";
        public const string DefaultKeysFile = "standardKeys.db";
        public const string EmojisFile = "emojis.txt";

        /// <summary>All files that make up a complete data folder.</summary>
        public static readonly string[] FileNames =
        {
            LanguageFile, DescriptionsFile, DefaultKeysFile, EmojisFile
        };

        private const string ResourcePrefix = "XML_Editor_WuffPad.Builtin.";

        /// <summary>
        /// Writes every built-in file into <see cref="AppData.Directory"/> unless a
        /// (possibly newer) copy already exists. Existing files are never touched.
        /// </summary>
        /// <returns>The number of files that had to be extracted.</returns>
        public static int EnsureExtracted()
        {
            int written = 0;
            AppData.EnsureDirectory();
            foreach (string fileName in FileNames)
            {
                string target = AppData.PathFor(fileName);
                if (File.Exists(target) && new FileInfo(target).Length > 0)
                {
                    continue;
                }
                if (TryExtract(fileName, target))
                {
                    written++;
                }
            }
            return written;
        }

        /// <summary>
        /// Extracts a single built-in file, overwriting the copy on disk.
        /// </summary>
        /// <returns>False if the file is not embedded in this build.</returns>
        public static bool Extract(string fileName, string pathTo)
        {
            Stream source = Open(fileName);
            if (source == null)
            {
                return false;
            }
            using (source)
            {
                string directory = Path.GetDirectoryName(pathTo);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
                // Write next to the target and move, so a failed extraction never
                // leaves a half written data file behind.
                string temporary = pathTo + ".tmp";
                using (FileStream fs = File.Create(temporary))
                {
                    source.CopyTo(fs);
                }
                if (File.Exists(pathTo))
                {
                    File.Delete(pathTo);
                }
                File.Move(temporary, pathTo);
            }
            return true;
        }

        /// <summary>True if a built-in copy of the file exists.</summary>
        public static bool Exists(string fileName)
        {
            return Open(fileName) != null;
        }

        private static bool TryExtract(string fileName, string pathTo)
        {
            try
            {
                return Extract(fileName, pathTo);
            }
            catch
            {
                // A locked or read-only folder must not stop the editor from opening.
                return false;
            }
        }

        private static Stream Open(string fileName)
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            return assembly.GetManifestResourceStream(ResourcePrefix + fileName);
        }
    }
}
using System.IO;
using System.Reflection;

namespace XML_Editor_WuffPad.Core
{
    /// <summary>
    /// The helper files WuffPad used to download from crazypokemondev.de.
    /// They are embedded in the executable and extracted on demand.
    /// </summary>
    /// <remarks>
    /// Rebuild the files with tools/make-builtin-resources.py from the upstream
    /// Werewolf langfiles (GreyWolfDev/Werewolf, GPL-3.0).
    /// </remarks>
    public static class BuiltinResources
    {
        public const string LanguageFile = "language.xml";
        public const string DescriptionsFile = "descriptions.dict";
        public const string DefaultKeysFile = "standardKeys.db";
        public const string EmojisFile = "emojis.txt";

        /// <summary>All files that make up a complete data folder.</summary>
        public static readonly string[] FileNames =
        {
            LanguageFile, DescriptionsFile, DefaultKeysFile, EmojisFile
        };

        private const string ResourcePrefix = "XML_Editor_WuffPad.Builtin.";

        /// <summary>
        /// Writes every built-in file into <see cref="AppData.Directory"/> unless a
        /// (possibly newer) copy already exists. Existing files are never touched.
        /// </summary>
        /// <returns>The number of files that had to be extracted.</returns>
        public static int EnsureExtracted()
        {
            int written = 0;
            try
            {
                AppData.EnsureDirectory();
                foreach (string fileName in FileNames)
                {
                    string target = AppData.PathFor(fileName);
                    if (File.Exists(target) && new FileInfo(target).Length > 0)
                    {
                        continue;
                    }
                    if (TryExtract(fileName, target))
                    {
                        written++;
                    }
                }
            }
            catch
            {
                // Whatever could not be extracted stays embedded; reading works
                // straight from the assembly (see ReadAllText).
            }
            return written;
        }

        /// <summary>
        /// Reads one of the built-in files directly from the assembly, bypassing
        /// the copy on disk.
        /// </summary>
        /// <returns>Null if the file is not embedded in this build.</returns>
        public static string ReadAllText(string fileName)
        {
            using (Stream stream = Open(fileName))
            {
                if (stream == null)
                {
                    return null;
                }
                using (StreamReader reader = new StreamReader(stream, System.Text.Encoding.UTF8, true))
                {
                    return reader.ReadToEnd();
                }
            }
        }

        /// <summary>
        /// Extracts a single built-in file, overwriting the copy on disk.
        /// </summary>
        /// <returns>False if the file is not embedded in this build.</returns>
        public static bool Extract(string fileName, string pathTo)
        {
            Stream source = Open(fileName);
            if (source == null)
            {
                return false;
            }
            using (source)
            {
                string directory = Path.GetDirectoryName(pathTo);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
                // Write next to the target and move, so a failed extraction never
                // leaves a half written data file behind.
                string temporary = pathTo + ".tmp";
                using (FileStream fs = File.Create(temporary))
                {
                    source.CopyTo(fs);
                }
                if (File.Exists(pathTo))
                {
                    File.Delete(pathTo);
                }
                File.Move(temporary, pathTo);
            }
            return true;
        }

        /// <summary>True if a built-in copy of the file exists.</summary>
        public static bool Exists(string fileName)
        {
            return Open(fileName) != null;
        }

        private static bool TryExtract(string fileName, string pathTo)
        {
            try
            {
                return Extract(fileName, pathTo);
            }
            catch
            {
                // A locked or read-only folder must not stop the editor from opening.
                return false;
            }
        }

        private static Stream Open(string fileName)
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            return assembly.GetManifestResourceStream(ResourcePrefix + fileName);
        }
    }
}
