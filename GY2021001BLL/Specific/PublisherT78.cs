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
using System.Text.Json;

namespace GuangYuan.GY001.BLL
{
    public class T78DataDto
    {
        public string GameId { get; set; }

        public string ChannelId { get; set; }

        public string AppId { get; set; }

        public string UserId { get; set; }

        public string SdkData { get; set; }

    }

    public class T78LoginReturnDto
    {
        public T78LoginReturnDto()
        {

        }

        public string Ret { get; set; }

        public string msg { get; set; }

        public T78LoginContentDto Content { get; set; } = new T78LoginContentDto();

        public string CData { get; set; }
    }

    public class T78LoginContentDto
    {
        public T78DataDto Data { get; set; } = new T78DataDto();

        public string AccessToken { get; set; }
    }

    /// <summary>
    /// 连接特定发行商的客户端服务。
    /// </summary>
    public class PublisherT78
    {
        const string AppSecret = "202cb962234w4ers2aa";    //基础加密矢量

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

        public T78LoginReturnDto Login(string sid)
        {
            var httpResult = PostAsync(new Dictionary<string, string>() { { "sid", sid }, }).Result;
            var resultString = httpResult.Content.ReadAsStringAsync().Result;
            var result=(T78LoginReturnDto)JsonSerializer.Deserialize(resultString, typeof(T78LoginReturnDto));
            return result;
        }

        public Task<HttpResponseMessage> PostAsync(IReadOnlyDictionary<string, string> pairs)
        {
            var dic = new Dictionary<string, string>(pairs);
            if (!dic.ContainsKey("gameId"))
                dic["gameId"] = string.Empty;

            if (!dic.ContainsKey("channelId"))
                dic["channelId"] = string.Empty;

            if (!dic.ContainsKey("appId"))
                dic["appId"] = string.Empty;

            if (!dic.ContainsKey("sid"))
                dic["sid"] = string.Empty;

            if (!dic.ContainsKey("extra"))
                dic["extra"] = string.Empty;

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
                sb.Append(AppSecret);
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
