#if NET5_0_OR_GREATER
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Net.Security;
using TraceLog;
using XDM.Core;
using XDM.Core.Security;

namespace XDM.Core.Clients.Http
{
    internal class DotNetHttpClient : IHttpClient
    {
        private readonly HttpClientHandler _handler;
        private readonly HttpClient _client;
        private bool disposed;

        public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(100);

        internal DotNetHttpClient(ProxyInfo? proxyInfo = null)
        {
            _handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = CertificateValidator.ValidateServerCertificate,
                UseCookies = true,
                AllowAutoRedirect = true,
                AutomaticDecompression = DecompressionMethods.All,
                PreAuthenticate = true,
                UseDefaultCredentials = true,
                MaxConnectionsPerServer = 100
            };

            if (proxyInfo != null)
            {
                ConfigureProxy(_handler, proxyInfo);
            }

            _client = new HttpClient(_handler);
            ConfigureClient(_client);
        }

        private void ConfigureClient(HttpClient client)
        {
            client.DefaultRequestHeaders.Add("User-Agent", "XDM/8.0");
            client.Timeout = this.Timeout;
        }

        private void ConfigureProxy(HttpClientHandler handler, ProxyInfo proxyInfo)
        {
            if (string.IsNullOrEmpty(proxyInfo.Host))
                return;

            handler.UseProxy = true;
            handler.Proxy = ProxyHelper.CreateWebProxy(proxyInfo);
        }

        public HttpRequest CreateGetRequest(Uri uri,
            Dictionary<string, List<string>>? headers = null,
            string? cookies = null,
            AuthenticationInfo? authentication = null)
        {
            var req = CreateRequest(uri, HttpMethod.Get, headers, cookies, authentication);
            return new HttpRequest { Session = new DotNetHttpSession { Request = req } };
        }

        public HttpRequest CreatePostRequest(Uri uri,
            Dictionary<string, List<string>>? headers = null,
            string? cookies = null,
            AuthenticationInfo? authentication = null,
            byte[]? body = null)
        {
            var req = CreateRequest(uri, HttpMethod.Post, headers, cookies, authentication);
            if (body != null)
            {
                req.Content = new ByteArrayContent(body);
                if (headers != null && headers.TryGetValue("Content-Type", out List<string>? values))
                {
                    if (values != null && values.Count > 0)
                    {
                        req.Content.Headers.ContentType = new MediaTypeHeaderValue(values[0]);
                    }
                }
            }
            return new HttpRequest { Session = new DotNetHttpSession { Request = req } };
        }

        private HttpRequestMessage CreateRequest(Uri uri, HttpMethod method,
            Dictionary<string, List<string>>? headers = null,
            string? cookies = null,
            AuthenticationInfo? authentication = null)
        {
            if (disposed)
            {
                throw new ObjectDisposedException("DotNetHttpClient");
            }

            var http = new HttpRequestMessage
            {
                Method = method,
                RequestUri = uri
            };

            if (headers != null)
            {
                foreach (var e in headers)
                {
                    SetHeader(http, e.Key, e.Value);
                }
            }
            if (cookies != null)
            {
                SetHeader(http, "Cookie", cookies);
            }
            return http;
        }

        public HttpResponse Send(HttpRequest request)
        {
            HttpRequestMessage r;
            HttpResponseMessage? response = null;
            if (request.Session == null)
            {
                throw new ArgumentNullException(nameof(request.Session));
            }
            if (request.Session is not DotNetHttpSession session)
            {
                throw new ArgumentNullException(nameof(request.Session));
            }
            if (session.Request == null)
            {
                throw new ArgumentNullException(nameof(session.Request));
            }
            r = session.Request;
            try
            {
                response = _client.SendAsync(r, HttpCompletionOption.ResponseHeadersRead).Result;
                response.EnsureSuccessStatusCode();
            }
            catch (HttpRequestException we)
            {
                Log.Debug(we, we.Message);
                response?.Dispose();
            }
            session.Response = response;
            return new HttpResponse { Session = session };
        }

        public void Dispose()
        {
            disposed = true;
            _client.Dispose();
            _handler.Dispose();
        }

        public void Close()
        {
            this.Dispose();
        }

        private void SetHeader(HttpRequestMessage request, string key, IEnumerable<string> values)
        {
            try
            {
                foreach (var value in values)
                {
                    if (!string.Equals(key, "Content-Type", StringComparison.InvariantCultureIgnoreCase))
                    {
                        request.Headers.TryAddWithoutValidation(key, value);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Debug(ex, "Error setting header value");
            }
        }

        private void SetHeader(HttpRequestMessage request, string key, string value)
        {
            try
            {
                if (!string.Equals(key, "Content-Type", StringComparison.InvariantCultureIgnoreCase))
                {
                    request.Headers.TryAddWithoutValidation(key, value);
                }
            }
            catch (Exception ex)
            {
                Log.Debug(ex, "Error setting header value");
            }
        }
    }
}
#endif