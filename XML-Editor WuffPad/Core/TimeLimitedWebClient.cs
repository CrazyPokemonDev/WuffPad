using System;
using System.Net;

namespace XML_Editor_WuffPad.Core
{
    /// <summary>
    /// <see cref="WebClient"/> with a request timeout.
    /// </summary>
    /// <remarks>
    /// Plain <see cref="WebClient"/> waits up to 100 seconds for a connection, which
    /// is far too long for the optional parts of WuffPad that touch the network.
    /// </remarks>
    internal sealed class TimeLimitedWebClient : WebClient
    {
        private readonly int timeoutMilliseconds;

        public TimeLimitedWebClient(int timeoutMilliseconds)
        {
            this.timeoutMilliseconds = timeoutMilliseconds;
        }

        public static TimeLimitedWebClient Create()
        {
            TimeLimitedWebClient client = new TimeLimitedWebClient(15000);
            client.Headers[HttpRequestHeader.UserAgent] = "WuffPad/1.0";
            client.Encoding = System.Text.Encoding.UTF8;
            return client;
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
