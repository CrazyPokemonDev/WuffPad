using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace XML_Editor_WuffPad.Core
{
    /// <summary>
    /// Optional refresh of the helper files from a remote server.
    /// </summary>
    /// <remarks>
    /// The original WuffPad downloaded these files from crazypokemondev.de on every
    /// start and refused to work when it could not. That server is gone, so the
    /// files now ship with the application (see <see cref="BuiltinResources"/>) and
    /// updating is strictly best effort: run in the background, never block the UI,
    /// never overwrite a local file unless the server really answered.
    ///
    /// Point <c>updateUrl</c> (App.config key <c>UpdateUrl</c> or the
    /// <c>WUFFPAD_UPDATE_URL</c> environment variable) at a folder that contains
    /// versions.txt plus the data files to receive the same files the old server
    /// provided.
    /// </remarks>
    public static class ResourceUpdater
    {
        /// <summary>App.config key that can override the update location.</summary>
        public const string UpdateUrlSetting = "UpdateUrl";

        /// <summary>Environment variable that can override the update location.</summary>
        public const string UpdateUrlVariable = "WUFFPAD_UPDATE_URL";

        private const string VersionFileName = "versions.txt";
        private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(15);
        private static int running;

        /// <summary>
        /// Starts a background update if an update location is configured.
        /// </summary>
        /// <param name="onFilesChanged">
        /// Invoked on a background thread with the names of the files that were
        /// replaced. May be null.
        /// </param>
        /// <returns>False if no update location is configured.</returns>
        public static bool TryStartBackgroundUpdate(Action<IList<string>> onFilesChanged)
        {
            string updateUrl = GetUpdateUrl();
            if (updateUrl == null)
            {
                return false;
            }
            if (Interlocked.Exchange(ref running, 1) == 1)
            {
                return false;
            }
            Task.Run(() =>
            {
                try
                {
                    IList<string> changed = Update(updateUrl);
                    if (changed.Count > 0 && onFilesChanged != null)
                    {
                        onFilesChanged(changed);
                    }
                }
                catch
                {
                    // Offline, server moved, folder read only: none of that is fatal.
                }
                finally
                {
                    Interlocked.Exchange(ref running, 0);
                }
            });
            return true;
        }

        /// <summary>
        /// Refreshes the data files from <paramref name="updateUrl"/>.
        /// </summary>
        /// <returns>The names of the files that were replaced.</returns>
        public static IList<string> Update(string updateUrl)
        {
            if (!updateUrl.EndsWith("/"))
            {
                updateUrl += "/";
            }
            AppData.EnsureDirectory();
            Dictionary<string, int> remoteVersions = ReadVersionNumbers(updateUrl + VersionFileName);
            Dictionary<string, int> localVersions = ReadLocalVersionNumbers();

            List<string> replaced = new List<string>();
            foreach (string fileName in BuiltinResources.FileNames)
            {
                int remote;
                if (!remoteVersions.TryGetValue(fileName, out remote))
                {
                    // Server does not know this file: leave the local copy alone.
                    continue;
                }
                int local;
                string localFile = AppData.PathFor(fileName);
                bool present = File.Exists(localFile) && new FileInfo(localFile).Length > 0;
                if (present && localVersions.TryGetValue(fileName, out local) && local >= remote)
                {
                    continue;
                }
                if (Download(updateUrl + fileName, localFile))
                {
                    replaced.Add(fileName);
                }
            }
            if (replaced.Count > 0 && remoteVersions.Count > 0)
            {
                WriteLocalVersionNumbers(remoteVersions);
            }
            return replaced;
        }

        /// <summary>
        /// The configured update location, or null when updates are disabled.
        /// </summary>
        public static string GetUpdateUrl()
        {
            string fromEnvironment = Environment.GetEnvironmentVariable(UpdateUrlVariable);
            if (!string.IsNullOrEmpty(fromEnvironment))
            {
                return fromEnvironment.Trim();
            }
            try
            {
                string fromConfig = System.Configuration.ConfigurationManager.AppSettings[UpdateUrlSetting];
                if (!string.IsNullOrEmpty(fromConfig))
                {
                    return fromConfig.Trim();
                }
            }
            catch
            {
                // No App.config (e.g. unit tests), fall through.
            }
            return null;
        }

        /// <summary>Parses a versions.txt ("name:version" per line).</summary>
        public static Dictionary<string, int> ReadVersionNumbers(string url)
        {
            Dictionary<string, int> versions = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            string raw = DownloadString(url);
            if (raw != null)
            {
                ParseVersionNumbers(raw, versions);
            }
            return versions;
        }

        /// <summary>Parses "name:version" lines, ignoring everything unreadable.</summary>
        public static void ParseVersionNumbers(string content, IDictionary<string, int> into)
        {
            foreach (string line in content.Split('\n'))
            {
                string trimmed = line.Trim();
                int separator = trimmed.LastIndexOf(':');
                if (separator <= 0 || separator == trimmed.Length - 1)
                {
                    continue;
                }
                string name = trimmed.Substring(0, separator).Trim();
                string number = trimmed.Substring(separator + 1).Trim();
                int value;
                if (name.Length > 0 && int.TryParse(number, out value))
                {
                    into[name] = value;
                }
            }
        }

        private static Dictionary<string, int> ReadLocalVersionNumbers()
        {
            Dictionary<string, int> versions = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            string path = AppData.PathFor(VersionFileName);
            try
            {
                if (File.Exists(path))
                {
                    ParseVersionNumbers(File.ReadAllText(path), versions);
                }
            }
            catch
            {
                // Unreadable version file: treat everything as stale but usable.
            }
            return versions;
        }

        private static void WriteLocalVersionNumbers(Dictionary<string, int> versions)
        {
            try
            {
                List<string> lines = new List<string>();
                foreach (KeyValuePair<string, int> pair in versions)
                {
                    lines.Add(pair.Key + ":" + pair.Value);
                }
                File.WriteAllLines(AppData.PathFor(VersionFileName), lines.ToArray());
            }
            catch
            {
                // Not being able to remember versions only costs a re-download next time.
            }
        }

        private static string DownloadString(string url)
        {
            try
            {
                using (WebClient client = CreateClient())
                {
                    return client.DownloadString(url);
                }
            }
            catch
            {
                return null;
            }
        }

        private static bool Download(string url, string pathTo)
        {
            string temporary = pathTo + ".download";
            try
            {
                using (WebClient client = CreateClient())
                {
                    client.DownloadFile(url, temporary);
                }
                if (!File.Exists(temporary) || new FileInfo(temporary).Length == 0)
                {
                    return false;
                }
                if (File.Exists(pathTo))
                {
                    File.Delete(pathTo);
                }
                File.Move(temporary, pathTo);
                return true;
            }
            catch
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
                    // Nothing left to clean up.
                }
                return false;
            }
        }

        private static WebClient CreateClient()
        {
            TimeLimitedWebClient client = new TimeLimitedWebClient((int)Timeout.TotalMilliseconds);
            client.Headers[HttpRequestHeader.UserAgent] = "WuffPad/1.0";
            return client;
        }

        /// <summary>
        /// WebClient without a request timeout would keep a start-up refresh alive
        /// for minutes on a bad connection.
        /// </summary>
        private sealed class TimeLimitedWebClient : WebClient
        {
            private readonly int timeoutMilliseconds;

            public TimeLimitedWebClient(int timeoutMilliseconds)
            {
                this.timeoutMilliseconds = timeoutMilliseconds;
            }

            protected override WebRequest GetWebRequest(Uri address)
            {
                WebRequest request = base.GetWebRequest(address);
                if (request != null)
                {
                    request.Timeout = timeoutMilliseconds;
                    HttpWebRequest httpRequest = request as HttpWebRequest;
                    if (httpRequest != null)
                    {
                        httpRequest.ReadWriteTimeout = timeoutMilliseconds;
                    }
                }
                return request;
            }
        }
    }
}
