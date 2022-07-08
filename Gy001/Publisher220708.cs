using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace Gy001
{
    /// <summary>
    /// 连接特定发行商的客户端服务。
    /// </summary>
    public class PublisherT78
    {
        public static void Config(IHttpClientBuilder builder)
        {
            builder.SetHandlerLifetime(TimeSpan.FromMinutes(5)).ConfigureHttpClient(c => c.BaseAddress = new Uri("https://krm.icebirdgame.com/user/token/v2"));
        }

        private readonly HttpClient _HttpClient;

        public PublisherT78(HttpClient httpClient)
        {
            _HttpClient = httpClient;
        }

        public void Login(string sid)
        {

        }
    }
}
