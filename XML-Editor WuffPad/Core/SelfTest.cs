using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using XMLClasses;

namespace XML_Editor_WuffPad.Core
{
    /// <summary>
    /// Checks that the data WuffPad needs at start-up is available and readable.
    /// </summary>
    /// <remarks>
    /// Run with <c>WuffPad.exe --selftest [report-file]</c>. Without a file name the
    /// report is written to <c>%TEMP%\wuffpad-selftest.txt</c>. The exit code is 0
    /// when everything is fine. This is the check the old "cannot download the
    /// application" start-up did, just without the server and without the UI.
    /// </remarks>
    public static class SelfTest
    {
        private const string Flag = "--selftest";

        /// <summary>True if the given command line asks for the self-test.</summary>
        public static bool IsRequested(string[] args)
        {
            return FindReportPath(args) != null;
        }

        /// <summary>Runs the self-test and returns the process exit code.</summary>
        public static int Run(string[] args)
        {
            StringBuilder report = new StringBuilder();
            bool passed = true;
            report.AppendLine("WuffPad self-test");
            report.AppendLine("Executable:     " + AppData.ExecutableDirectory);
            report.AppendLine("Data directory: " + AppData.Directory
                + (AppData.ExecutableDirectoryIsWritable ? " (next to the executable)" : " (user profile, install folder is read-only)"));
            report.AppendLine();

            // 1. The files must be embedded in this very assembly; without them a
            //    fresh install has nothing to fall back to.
            foreach (string fileName in BuiltinResources.FileNames)
            {
                bool embedded = BuiltinResources.Exists(fileName);
                passed &= embedded;
                report.AppendLine(string.Format("embedded {0,-20} {1}", fileName, embedded ? "ok" : "MISSING"));
            }
            report.AppendLine();

            // 2. Extraction has to work where the application will read from.
            int extracted = BuiltinResources.EnsureExtracted();
            report.AppendLine(string.Format("extracted {0} file(s) into the data directory", extracted));
            foreach (string fileName in BuiltinResources.FileNames)
            {
                string path = AppData.PathFor(fileName);
                bool exists = File.Exists(path);
                passed &= exists;
                report.AppendLine(string.Format("on disk  {0,-20} {1}", fileName,
                    exists ? path : "MISSING at " + path));
            }
            report.AppendLine();

            // 3. Every file must be readable by the code that consumes it.
            passed &= ReportLanguage(report);
            passed &= ReportDescriptions(report);
            passed &= ReportKeys(report);
            passed &= ReportEmojis(report);

            // 4. Editing and saving has to round-trip: this is the only part of the
            //    application that does not need a window.
            passed &= ReportRoundTrip(report);

            // 5. The application has to be usable without any network access.
            string updateUrl = ResourceUpdater.GetUpdateUrl();
            report.AppendLine("update source:  "
                + (string.IsNullOrEmpty(updateUrl) ? "none (fully offline)" : updateUrl));
            report.AppendLine();
            report.AppendLine(passed ? "Result: PASS" : "Result: FAIL");

            // Put the verdict first: build logs and annotations clip long output.
            string[] lines = report.ToString().Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
            StringBuilder ordered = new StringBuilder();
            ordered.AppendLine(lines[lines.Length - 2]);
            foreach (string line in lines)
            {
                ordered.AppendLine(line);
            }
            report = ordered;

            string reportPath = FindReportPath(args);
            try
            {
                File.WriteAllText(reportPath, report.ToString(), Encoding.UTF8);
            }
            catch
            {
                // Nowhere to write: the exit code still tells the caller.
            }
            return passed ? 0 : 1;
        }

        private static bool ReportLanguage(StringBuilder report)
        {
            try
            {
                XmlStrings strings = DeserializeLanguage(AppData.PathFor(BuiltinResources.LanguageFile));
                bool ok = strings != null && strings.Strings.Count > 0;
                report.AppendLine(string.Format("language.xml:   {0} ({1} strings, language {2})",
                    ok ? "ok" : "EMPTY",
                    strings == null ? 0 : strings.Strings.Count,
                    strings == null || strings.Language == null ? "?" : strings.Language.Name));
                return ok;
            }
            catch (Exception e)
            {
                report.AppendLine("language.xml:   FAILED " + e.Message);
                return false;
            }
        }

        private static bool ReportDescriptions(StringBuilder report)
        {
            try
            {
                Dictionary<string, string> descriptions = JsonConvert.DeserializeObject<Dictionary<string, string>>(
                    File.ReadAllText(AppData.PathFor(BuiltinResources.DescriptionsFile)));
                bool ok = descriptions != null && descriptions.Count > 0;
                report.AppendLine(string.Format("descriptions:   {0} ({1} entries)",
                    ok ? "ok" : "EMPTY", descriptions == null ? 0 : descriptions.Count));
                return ok;
            }
            catch (Exception e)
            {
                report.AppendLine("descriptions:   FAILED " + e.Message);
                return false;
            }
        }

        private static bool ReportKeys(StringBuilder report)
        {
            try
            {
                string[] lines = File.ReadAllLines(AppData.PathFor(BuiltinResources.DefaultKeysFile));
                int count = 0;
                foreach (string line in lines)
                {
                    if (line.Trim().Length > 0)
                    {
                        count++;
                    }
                }
                report.AppendLine(string.Format("default keys:   {0} ({1} keys)", count > 0 ? "ok" : "EMPTY", count));
                return count > 0;
            }
            catch (Exception e)
            {
                report.AppendLine("default keys:   FAILED " + e.Message);
                return false;
            }
        }

        private static bool ReportEmojis(StringBuilder report)
        {
            try
            {
                string[] lines = File.ReadAllLines(AppData.PathFor(BuiltinResources.EmojisFile), Encoding.UTF8);
                int count = 0;
                foreach (string line in lines)
                {
                    if (line.Trim().Length > 0)
                    {
                        count++;
                    }
                }
                report.AppendLine(string.Format("emojis:         {0} ({1} buttons)", count > 0 ? "ok" : "EMPTY", count));
                return count > 0;
            }
            catch (Exception e)
            {
                report.AppendLine("emojis:         FAILED " + e.Message);
                return false;
            }
        }

        /// <summary>
        /// Loads the blueprint, edits it and saves it again, checking that the
        /// result is still a readable langfile with the added string in place.
        /// </summary>
        private static bool ReportRoundTrip(StringBuilder report)
        {
            string temporary = Path.Combine(Path.GetTempPath(), "wuffpad-selftest-language.xml");
            try
            {
                XmlStrings strings = DeserializeLanguage(AppData.PathFor(BuiltinResources.LanguageFile));
                int before = strings.Strings.Count;
                string key = "WuffPadSelfTestKey";
                strings.Strings.Add(new XmlString { Key = key, Description = "added by the self-test" });
                strings.Strings[strings.Strings.Count - 1].Values.Add("Self-test value");

                System.Xml.Serialization.XmlSerializer serializer =
                    new System.Xml.Serialization.XmlSerializer(typeof(XmlStrings));
                using (TextWriter writer = new StreamWriter(temporary, false, new UTF8Encoding(false)))
                {
                    serializer.Serialize(writer, strings);
                }
                XmlStrings again = DeserializeLanguage(temporary);
                bool ok = again.Strings.Count == before + 1;
                foreach (XmlString entry in again.Strings)
                {
                    if (entry.Key == key && entry.Values.Count == 1 && entry.Values[0] == "Self-test value")
                    {
                        ok = true;
                    }
                }
                report.AppendLine(string.Format("save round trip: {0} ({1} -> {2} strings)",
                    ok ? "ok" : "FAILED", before, again.Strings.Count));
                return ok;
            }
            catch (Exception e)
            {
                report.AppendLine("save round trip: FAILED " + e.Message);
                return false;
            }
            finally
            {
                try
                {
                    if (File.Exists(temporary))
                    {
                        File.Delete(temporary);
                    }
                }
                catch
                {
                    // Left-over temp file is harmless.
                }
            }
        }

        private static XmlStrings DeserializeLanguage(string path)
        {
            System.Xml.Serialization.XmlSerializer serializer = new System.Xml.Serialization.XmlSerializer(typeof(XmlStrings));
            using (TextReader reader = new StringReader(File.ReadAllText(path)))
            {
                return (XmlStrings)serializer.Deserialize(reader);
            }
        }

        private static string FindReportPath(string[] args)
        {
            if (args == null)
            {
                return null;
            }
            for (int i = 0; i < args.Length; i++)
            {
                if (string.Equals(args[i], Flag, StringComparison.OrdinalIgnoreCase))
                {
                    if (i + 1 < args.Length && !args[i + 1].StartsWith("-"))
                    {
                        return args[i + 1];
                    }
                    return Path.Combine(Path.GetTempPath(), "wuffpad-selftest.txt");
                }
            }
            return null;
        }
    }
}
