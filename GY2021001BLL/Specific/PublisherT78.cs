using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Text;
using Microsoft.Extensions.ObjectPool;
using System.Security.Cryptography;

namespace GuangYuan.GY001.BLL
{
    /// <summary>
    /// 连接特定发行商的客户端服务。
    /// </summary>
    public class PublisherT78
    {
        public static void Config(IHttpClientBuilder builder)
        {
            builder.SetHandlerLifetime(TimeSpan.FromMinutes(5)).ConfigureHttpClient(c =>
            {
                c.DefaultRequestHeaders.Add("ContentType", "application/x-www-form-urlencoded");
            });
        }

        private readonly HttpClient _HttpClient;

        string _Url = "https://krm.icebirdgame.com/user/token/v2";

        public PublisherT78(HttpClient httpClient)
        {
            _HttpClient = httpClient;
        }

        public void Login(string sid)
        {
            var httpResult = Post(new Dictionary<string, string>() { { "sid", "13" }, }).Result;
            var resultString = httpResult.Content.ReadAsStringAsync().Result;
        }

        public Task<HttpResponseMessage> Post(IReadOnlyDictionary<string, string> pairs)
        {
            var dic = new Dictionary<string, string>(pairs);
            dic["sign"] = GetSignature(dic);
            FormUrlEncodedContent content = new FormUrlEncodedContent(dic);
            //content.Headers.ContentType = new MediaTypeHeaderValue("application/x-www-form-urlencoded");
            return _HttpClient.PostAsync(_Url, content);
        }

        /// <summary>
        /// 获取签名。
        /// </summary>
        /// <param name="dic"></param>
        /// <returns></returns>
        public string GetSignature(IReadOnlyDictionary<string, string> dic)
        {
            const string app_secret = "202cb962234w4ers2aa";    //基础矢量
            var sb = StringBuilderPool.Shared.Get();
            byte[] ary;
            try
            {
                foreach (var item in dic.OrderBy(c => c.Key))
                {
                    sb.Append(item.Key);
                    sb.Append("=");
                    sb.Append(item.Value);
                }
                sb.Append(app_secret);
                ary = Encoding.UTF8.GetBytes(sb.ToString());
                //ary = Encoding.UTF8.GetBytes("appId=channelId=1extra=gameId=1sid=appSecret");

                using var md5 = MD5.Create();
                var hash = md5.ComputeHash(ary, 0, ary.Length);
                var result = BitConverter.ToString(hash).Replace("-", string.Empty).ToLower();
                return result;
            }
            finally
            {
                StringBuilderPool.Shared.Return(sb);
            }
        }
    }
}
