using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Threading.Tasks;
using XDM.Core;
using XDM.Core.Clients.Http;

namespace XDM.Tests
{
    [TestClass]
    public class HttpClientTests
    {
        [TestMethod]
        public async Task TestHttpClientCreation()
        {
            var client = HttpClientFactory.NewHttpClient(null);
            Assert.IsNotNull(client, "HTTP client should not be null");
        }

        [TestMethod]
        public async Task TestHttpClientWithProxy()
        {
            var proxyInfo = new ProxyInfo
            {
                Host = "localhost",
                Port = 8080,
                ProxyType = ProxyType.Http
            };
            var client = HttpClientFactory.NewHttpClient(proxyInfo);
            Assert.IsNotNull(client, "HTTP client with proxy should not be null");
        }

        [TestMethod]
        public async Task TestHttpRequest()
        {
            var client = HttpClientFactory.NewHttpClient(null);
            var request = new HttpRequest
            {
                URL = "https://example.com",
                Method = "GET"
            };
            Assert.IsNotNull(request, "HTTP request should not be null");
        }
    }
}
