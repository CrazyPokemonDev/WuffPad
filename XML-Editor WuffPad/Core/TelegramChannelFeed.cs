using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace XML_Editor_WuffPad.Core
{
    /// <summary>
    /// Reads the public message feed of a Telegram channel.
    /// </summary>
    /// <remarks>
    /// WuffPad used to render #closedlist and #underdev, two channel pins served by
    /// its own PHP backend. That backend is gone, but the channels are public, so
    /// the same text can be taken straight from <c>https://t.me/s/&lt;channel&gt;</c>
    /// (the preview version of a channel, no login required).
    /// </remarks>
    public static class TelegramChannelFeed
    {
        /// <summary>Preview page of a public channel.</summary>
        public static string PreviewUrl(string channel)
        {
            return "https://t.me/s/" + channel;
        }

        /// <summary>
        /// Downloads a channel's preview page.
        /// </summary>
        /// <returns>Null when the channel cannot be read.</returns>
        public static string DownloadChannel(string channel)
        {
            try
            {
                using (System.Net.WebClient client = new System.Net.WebClient())
                {
                    client.Headers[System.Net.HttpRequestHeader.UserAgent] = "WuffPad/1.0";
                    client.Encoding = Encoding.UTF8;
                    return client.DownloadString(PreviewUrl(channel));
                }
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Turns a t.me channel page into the text that used to be shown in the
        /// #closedlist / #underdev window.
        /// </summary>
        /// <returns>Null when the page contains no usable messages.</returns>
        public static string GetMessages(string html)
        {
            if (string.IsNullOrEmpty(html))
            {
                return null;
            }
            List<string> messages = ExtractMessages(html);
            if (messages.Count == 0)
            {
                return null;
            }
            // Telegram renders newest first; the old list showed the pins in the
            // order they were written, so reverse them.
            messages.Reverse();
            StringBuilder builder = new StringBuilder();
            foreach (string message in messages)
            {
                builder.AppendLine("<p>");
                builder.Append(message.Replace("\n", "<br>"));
                builder.AppendLine("</p>");
            }
            return builder.ToString();
        }

        /// <summary>
        /// Extracts the text of every message in a t.me preview page.
        /// </summary>
        public static List<string> ExtractMessages(string html)
        {
            List<string> messages = new List<string>();
            foreach (Match block in Regex.Matches(html,
                "<div class=\"tgme_widget_message_text[^\"]*\"[^>]*>(.*?)</div>",
                RegexOptions.Singleline))
            {
                string text = ToPlainText(block.Groups[1].Value);
                if (!string.IsNullOrEmpty(text))
                {
                    messages.Add(text);
                }
            }
            return messages;
        }

        /// <summary>Converts the small HTML subset Telegram uses into plain text.</summary>
        public static string ToPlainText(string html)
        {
            if (string.IsNullOrEmpty(html))
            {
                return string.Empty;
            }
            string text = html;
            text = Regex.Replace(text, "\\s*<br\\s*/?>\\s*", "\n", RegexOptions.IgnoreCase);
            text = Regex.Replace(text, "</?p\\b[^>]*>", "\n", RegexOptions.IgnoreCase);
            text = Regex.Replace(text, "</?div\\b[^>]*>", "\n", RegexOptions.IgnoreCase);
            text = Regex.Replace(text, "<[^>]+>", string.Empty);
            text = System.Net.WebUtility.HtmlDecode(text);
            // Collapse the whitespace Telegram puts between formatted spans.
            List<string> lines = new List<string>();
            foreach (string line in text.Split('\n'))
            {
                lines.Add(Regex.Replace(line, "[ \t]+", " ").Trim());
            }
            text = string.Join("\n", lines.ToArray());
            text = Regex.Replace(text, "\n{3,}", "\n\n");
            return text.Trim();
        }
    }
}
