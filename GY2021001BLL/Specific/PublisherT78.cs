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
using System.Collections.Concurrent;
using System.Buffers;

namespace GuangYuan.GY001.BLL
{
#pragma warning disable IDE1006 // 命名样式

    public class T78DataDto
    {
        public string GameId { get; set; }

        public int ChannelId { get; set; }

        public string AppId { get; set; }

        public string UserId { get; set; }

        public object SdkData { get; set; }

        public string AccessToken { get; set; }
    }

    public class T78LoginReturnDto
    {
        public T78LoginReturnDto()
        {

        }

        public string Ret { get; set; }

        public string msg { get; set; }

        public T78LoginContentDto Content { get; set; } = new T78LoginContentDto();

        public string ResultString { get; set; }
    }

    public class T78LoginContentDto
    {
        public T78DataDto Data { get; set; } = new T78DataDto();

        public object CData { get; set; }

    }

    /// <summary>
    /// 付费回调的入口参数。
    /// HTTP POST（application/x-www-form-urlencoded）。
    /// </summary>
    public class PayCallbackFromT78ParamsDto
    {
        /// <summary>
        /// 默认构造函数。
        /// </summary>
        public PayCallbackFromT78ParamsDto()
        {

        }

        /// <summary>
        /// 游戏ID。
        /// </summary>
        public string gameId { get; set; }

        /// <summary>
        /// 渠道ID。
        /// </summary>
        public string channelId { get; set; }

        /// <summary>
        /// 游戏包ID。
        /// </summary>
        public string appId { get; set; }

        /// <summary>
        /// 用户ID。
        /// </summary>
        public string userId { get; set; }

        /// <summary>
        /// 游戏方的订单ID。
        /// </summary>
        public string cpOrderId { get; set; }

        /// <summary>
        /// 订单ID。
        /// </summary>
        public string bfOrderId { get; set; }

        /// <summary>
        /// 渠道的订单ID。
        /// </summary>
        public string channelOrderId { get; set; }

        /// <summary>
        /// 金额。单位:分。
        /// </summary>
        public int money { get; set; }

        /// <summary>
        /// 支付透参。
        /// </summary>
        public string callbackInfo { get; set; }

        /// <summary>
        /// 订单状态。0--支付失败，1—支付成功
        /// </summary>
        public int orderStatus { get; set; }

        /// <summary>
        /// 渠道自定义信息。目前不支持，固定为空字符串。
        /// </summary>
        public string channelInfo { get; set; }

        /// <summary>
        /// 币种。
        /// </summary>
        public string currency { get; set; }

        /// <summary>
        /// 商品id。
        /// </summary>
        public string product_id { get; set; }

        /// <summary>
        /// 区服id。
        /// </summary>
        public string server_id { get; set; }

        /// <summary>
        /// 角色id。
        /// </summary>
        public string game_role_id { get; set; }

        /// <summary>
        /// 时间戳。
        /// </summary>
        public string time { get; set; }

        /// <summary>
        /// 签名。
        /// </summary>
        public string sign { get; set; }
    }

    /// <summary>
    /// 连接特定发行商的客户端服务。
    /// </summary>
    public class PublisherT78
    {
        const string AppSecret = "c73f8a6a27cb3e13c4bf455bef422cdb";    //基础加密矢量

        private readonly HttpClient _HttpClient;
        readonly string _Url = "https://krm.icebirdgame.com/user/token/v2";

        static readonly ConcurrentDictionary<string, (PayCallbackFromT78ParamsDto, bool)> _Dic = new ConcurrentDictionary<string, (PayCallbackFromT78ParamsDto, bool)>();

        public PublisherT78(HttpClient httpClient)
        {
            _HttpClient = httpClient;
        }

        /// <summary>
        /// 登录账号。
        /// </summary>
        /// <param name="sid"></param>
        /// <returns></returns>
        public T78LoginReturnDto Login(string sid)
        {
            var options = new JsonSerializerOptions() { PropertyNameCaseInsensitive = true };
            var httpResult = PostAsync(new Dictionary<string, string>() { { "sid", sid }, }).Result;
            var resultString = httpResult.Content.ReadAsStringAsync().Result;
            var result = (T78LoginReturnDto)JsonSerializer.Deserialize(resultString, typeof(T78LoginReturnDto), options);
            result.ResultString = resultString;
            return result;
        }

        /// <summary>
        /// 发行商的付费信息。
        /// </summary>
        /// <param name="dto"></param>
        public void Pay(PayCallbackFromT78ParamsDto dto)
        {
            var item = _Dic.AddOrUpdate(dto.cpOrderId, c => (dto, false), (key, ov) => (dto, ov.Item2));
            if (item.Item1 != null && item.Item2)
                ;
        }

        public void Pay(string orderId)
        {
            var item = _Dic.AddOrUpdate(orderId, c => (null, true), (key, ov) => (ov.Item1, true));
            if (item.Item1 != null && item.Item2)
                ;
        }

        #region 基础功能

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
                    sb.Append('=');
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
        #endregion 基础功能
    }

    public static class PublisherT78Extensions
    {
        public static IHttpClientBuilder AddPublisherT78(this IServiceCollection services)
        {
            return services.AddHttpClient<PublisherT78, PublisherT78>().SetHandlerLifetime(TimeSpan.FromMinutes(5)).ConfigureHttpClient(c =>
            {
                c.DefaultRequestHeaders.Add("ContentType", "application/x-www-form-urlencoded");
            });
        }
    }

#pragma warning restore IDE1006 // 命名样式

}
