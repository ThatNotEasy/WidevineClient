using System;
using ProtoBuf;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using Org; // Add this line if Org namespace is required
using Newtonsoft.Json; // Add this line if Newtonsoft namespace is required
using Org.BouncyCastle.Security; // Add this line if BouncyCastle namespace is required

namespace WidevineClient
{
    class HttpUtil
    {
        private static readonly HttpClient Client = new HttpClient(new HttpClientHandler
        {
            AllowAutoRedirect = true,
            // Proxy = null
        });

        public static byte[] PostData(string url, Dictionary<string, string> headers, string postData)
        {
            var mediaType = postData.StartsWith("{") ? "application/json" : "application/x-www-form-urlencoded";
            var content = new StringContent(postData, Encoding.UTF8, mediaType);

            var response = Post(url, headers, content);
            return response.Content.ReadAsByteArrayAsync().Result;
        }

        public static byte[] PostData(string url, Dictionary<string, string> headers, byte[] postData)
        {
            var content = new ByteArrayContent(postData);

            var response = Post(url, headers, content);
            return response.Content.ReadAsByteArrayAsync().Result;
        }

        public static byte[] PostData(string url, Dictionary<string, string> headers, Dictionary<string, string> postData)
        {
            var content = new FormUrlEncodedContent(postData);

            var response = Post(url, headers, content);
            return response.Content.ReadAsByteArrayAsync().Result;
        }

        public static string GetWebSource(string url, Dictionary<string, string> headers = null)
        {
            var response = Get(url, headers);
            var bytes = response.Content.ReadAsByteArrayAsync().Result;
            return Encoding.UTF8.GetString(bytes);
        }

        public static byte[] GetBinary(string url, Dictionary<string, string> headers = null)
        {
            var response = Get(url, headers);
            return response.Content.ReadAsByteArrayAsync().Result;
        }

        public static string GetString(byte[] bytes)
        {
            return Encoding.UTF8.GetString(bytes);
        }

        private static HttpResponseMessage Get(string url, Dictionary<string, string> headers = null)
        {
            var request = new HttpRequestMessage
            {
                RequestUri = new Uri(url),
                Method = HttpMethod.Get
            };

            AddHeaders(request, headers);

            return Send(request);
        }

        private static HttpResponseMessage Post(string url, Dictionary<string, string> headers, HttpContent content)
        {
            var request = new HttpRequestMessage
            {
                RequestUri = new Uri(url),
                Method = HttpMethod.Post,
                Content = content
            };

            AddHeaders(request, headers);

            return Send(request);
        }

        private static void AddHeaders(HttpRequestMessage request, Dictionary<string, string> headers)
        {
            if (headers != null)
            {
                foreach (var header in headers)
                {
                    request.Headers.TryAddWithoutValidation(header.Key, header.Value);
                }
            }
        }

        private static HttpResponseMessage Send(HttpRequestMessage request)
        {
            return Client.SendAsync(request).Result;
        }
    }
}
