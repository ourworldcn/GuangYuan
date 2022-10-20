﻿using Microsoft.Extensions.DependencyInjection;
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
using OW.Game;
using System.ComponentModel;
using Castle.Core.Logging;
using Microsoft.Extensions.Logging;
using GuangYuan.GY001.UserDb;
using Microsoft.EntityFrameworkCore;

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
    /// 连接特定发行商的客户端服务。
    /// </summary>
    public class PublisherT78
    {
        const string AppSecret = "c73f8a6a27cb3e13c4bf455bef422cdb";    //基础加密矢量

        private readonly HttpClient _HttpClient;
        readonly string _Url = "https://krm.icebirdgame.com/user/token/v2";

        static readonly ConcurrentDictionary<string, (PayCallbackT78ParamsDto, bool)> _Dic = new ConcurrentDictionary<string, (PayCallbackT78ParamsDto, bool)>();

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

    #region 付费回调

    /// <summary>
    /// 充值回调接口接收的参数。
    /// 付费回调的入口参数。
    /// HTTP POST（application/x-www-form-urlencoded）。
    /// </summary>
    public class PayCallbackT78ParamsDto
    {
        public PayCallbackT78ParamsDto()
        {

        }

        /// <summary>
        /// 游戏ID。
        /// </summary>
        public string GameId { get; set; }

        /// <summary>
        /// 渠道ID。
        /// </summary>
        public int ChannelId { get; set; }

        /// <summary>
        /// 游戏包ID。
        /// </summary>
        public string AppId { get; set; }

        /// <summary>
        /// 用户ID。
        /// </summary>
        public string UserId { get; set; }

        /// <summary>
        /// 游戏方的订单ID。
        /// </summary>
        public string cpOrderId { get; set; }

        /// <summary>
        /// 订单Id。
        /// </summary>
        public string bfOrderId { get; set; }

        /// <summary>
        /// 渠道的订单ID。
        /// </summary>
        public string channelOrderId { get; set; }

        /// <summary>
        /// 金额，单位：分。
        /// </summary>
        public int money { get; set; }

        /// <summary>
        /// 支付透参。
        /// </summary>
        public string callbackInfo { get; set; }

        /// <summary>
        /// 0--支付失败
        /// 1—支付成功
        /// </summary>
        public string orderStatus { get; set; }

        /// <summary>
        /// 目前不支持，固定为空字符串。
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
        /// 签名。签名算法见下文。
        /// </summary>
        public string sign { get; set; }
    }

    /// <summary>
    /// 付费回调的返回类。
    /// </summary>
    public class PayCallbackT78ReturnDto
    {
        /// <summary>
        /// 0=成功，表示游戏服务器成功接收了该次充值结果通知,注意是0为成功
        /// 1=失败，表示游戏服务器无法接收或识别该次充值结果通知，如：签名检验不正确、游戏服务器接收失败
        /// </summary>
        public int ret { get; set; }
    }

    public class T78PayCallbackCommand : GameCommandBase
    {
        public PayCallbackT78ParamsDto Params { get; set; }

        /// <summary>
        /// "1"表示沙箱；其他表示正式。
        /// </summary>
        public string SandBox { get; set; }

        /// <summary>
        /// 支付方式：
        /// "mycard"表示mycard，"google"表示google-play支付，"mol"表示mol支付，"apple"表示苹果支付，“onestore”韩国onestore商店支付，“samsung”三星支付
        /// </summary>
        public string PayType { get; set; }
    }

    public class T78PayCallbackCommandHandler : GameCommandHandlerBase<T78PayCallbackCommand>
    {
        public T78PayCallbackCommandHandler(IServiceProvider service)
        {
            _Service = service;
        }

        IServiceProvider _Service;

        /// <summary>
        /// 获取指定对象的属性字典。
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static Dictionary<string, string> GetDictionary(object obj)
        {
            var result = AutoClearPool<Dictionary<string, string>>.Shared.Get();
            var pis = TypeDescriptor.GetProperties(obj).OfType<PropertyDescriptor>();
            foreach (var pi in pis)
            {
                result[pi.Name] = pi.GetValue(obj).ToString();
            }
            return result;
        }

        /// <summary>
        /// 校验签名。
        /// </summary>
        /// <param name="command"></param>
        /// <returns></returns>
        bool Verify(T78PayCallbackCommand command)
        {
            var t78 = _Service.GetRequiredService<PublisherT78>();
            var dic = GetDictionary(command.Params);
            dic.Remove(nameof(PayCallbackT78ParamsDto.sign));
            var sign = t78.GetSignature(dic);   //取签名
            AutoClearPool<Dictionary<string, string>>.Shared.Return(dic);
            var result = sign == command.Params.sign;
            if (!result)
            {
                command.ErrorCode = ErrorCodes.ERROR_BAD_ARGUMENTS;
                command.DebugMessage = $"签名错误，入参：{command.Params.sign}，计算为：{sign}。";
            }
            return result;
        }

        IDisposable GetUser(string userId, DbContext db, out GameChar gameChar)
        {
            var world = _Service.GetRequiredService<VWorld>();
            var coll = from tmp in db.Set<GameItem>()
                       where tmp.ExtraGuid == ProjectConstant.T78PublisherSlotTId && tmp.ExtraString == userId
                       select tmp.OwnerId;
            var charId = coll.FirstOrDefault();
            if (!charId.HasValue)
            {
                OwHelper.SetLastError(ErrorCodes.ERROR_BAD_ARGUMENTS);
                OwHelper.SetLastErrorMessage($"找不到指定的角色，T78的角色Id={userId}");
                gameChar = null;
                return null;
            }
            var gcm = _Service.GetRequiredService<GameCharManager>();
            var dw = gcm.LockOrLoad(charId.Value, out var gu);
            if (dw is null)
            {
                gameChar = null;
                return null;
            }
            gameChar = gu.CurrentChar;
            OwHelper.SetLastError(ErrorCodes.NO_ERROR);
            return dw;
        }

        public void RecodeAccount()
        {
            return;
        }

        public override void Handle(T78PayCallbackCommand command)
        {
            if (!Verify(command))  //若签名不正确
                return;
            var db = _Service.GetRequiredService<GY001UserContext>();
            using var dw = GetUser(command.Params.UserId, db, out var gc);
            if (dw is null)
            {
                command.FillErrorFromWorld();
                return;
            }
            //因不可拒绝，直接记录

            //处理购买，若有错则不增加道具/代币
            var userId = command.Params.UserId;
        }
    }

    #endregion 付费回调

#pragma warning restore IDE1006 // 命名样式

}
