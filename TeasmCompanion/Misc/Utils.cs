using System;
using System.Net;
using System.Net.Http;

namespace TeasmCompanion
{
    public class Utils
    {
        public static HttpClient CreateHttpClient()
        {
            HttpClientHandler handler = new HttpClientHandler()
            {
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate | DecompressionMethods.Brotli
            };
            return new HttpClient(handler);
        }

        public static DateTime JavaScriptUtcMsToDateTime(long javaScriptMilliseconds)
        {
            return new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                .AddMilliseconds(javaScriptMilliseconds);
            //.ToLocalTime();
        }
    }
}
