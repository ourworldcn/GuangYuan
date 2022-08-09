using GuangYuan.GY001.UserDb;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.ObjectPool;
using OW.Game;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace GuangYuan.GY001.BLL.Specific
{
    /// <summary>
    /// 
    /// </summary>
    public class PublisherT89
    {
        /// <summary>
        /// 发行方公钥。
        /// </summary>
        const string PublisherKeyBase64String = "MIICdwIBADANBgkqhkiG9w0BAQEFAASCAmEwggJdAgEAAoGBAJnxtvfMDDm13i69RXNTiUd4YCeGyTfCaX5BjnRFhisK0BEiszIdczR59zkSu2I+rUxp0u6/RvVMoWr0HDPaJxZU62rnTEKBPhSCv/5vSnVf2wemO3rBDfhYUtUvXvdsNm0Obm/yc8q+VnYKKMUw2m9w/KHAMv9eRfG+fJu6M0xbAgMBAAECgYEAkA4XyHQh8JMWcahd6BiAuueTGwnPG6WKcEJKWtWAQqDgqVhOVZQnbTDAvqCfxsSeCIZyg0Vs0ji+SEZcac03iwOeu0p/COQdCgC1HiDRiLB7bAiYX6ARpLxNJ0SdBZkO9evepwYf/MQWCosQLJQT0nK1PVpD0sliJPpZzihPUdkCQQDJ6t6K1xqCYcqu8UChvHXIXBJhUBjSZLl4zqG5MSnPB/xaXurLUW3voUZMWgAbVdKq26XxFow2IStFER50GhS9AkEAwy1m6HJ0LTzdkz4UiJd9HlBzFopx7B4pVF14FavJxfQ92zwE4nICbJt4S5cpJn9hN2Nxb440ZQoNxjIAmSQS9wJAWHkdQeC2sOKAmN8E3tlhEoWlWBZsieOwkQqZOjgchaaOIFrurl/jt9gGoXXeDhwwzsSnlVe34A/wkbbKjTzQUQJBAImAANGdYE9GY+sJfznDT3f9wTaODjgD/6XZu3G49osoyDMdE2vcWezdkl/efDUM5BY7Tn0Ct9K+xHluXJGGon0CQA3kHn05D7uQzNiuBQhP8eegOPHEnAOdoYVQwHH/WRA8GUQV/N3fTTs733HYCAzUkxMpJNtmBPNUU/eMe9YAiaQ=";

        const string PublisherPublishKeyBase64String = "MIGfMA0GCSqGSIb3DQEBAQUAA4GNADCBiQKBgQCEsbYHbJE7eCeH01L5DKTPguOmt5rWumZwriYyxWQ4ea/T6udkSSkELPLWUXr7zmPJQBsV5GhxwrRNuH3449nfD1yfIepx5tpuhRcOUO4DWHDqBGhv0vwDDt0PSl35KkyZMTDchTp4BC2uxm7gjStUoQpQMv1WjAPUnBoZ6bbAMwIDAQAB";

        /// <summary>
        /// 发行方的RSA公钥。
        /// </summary>
        static readonly byte[] PublisherKey = Convert.FromBase64String(PublisherKeyBase64String);

        static readonly byte[] PublisherPublishKey = Convert.FromBase64String(PublisherPublishKeyBase64String);

        const string myKey = "";

        /// <summary>
        /// 全球地区。
        /// </summary>
        const string GlobalAddress = "https://mapi.perfectworldgames.com";

        /// <summary>
        /// 港澳台艾玩公司。
        /// </summary>
        const string GatAddress = "https://mapitw.playcomb.com";

        private readonly HttpClient _HttpClient;

        public PublisherT89(HttpClient httpClient)
        {
            _HttpClient = httpClient;
        }

        /// <summary>
        /// 获取签名后的字符串。
        /// </summary>
        /// <param name="dic"></param>
        /// <returns></returns>
        public string GetSignString(IReadOnlyDictionary<string, string> dic)
        {
            var coll = dic.OrderBy(c => c.Key).Select(c => $"{c.Key}={c.Value}");
            var result = string.Join('&', coll);
            result = SHA1WithRSA.Sign(result, PublisherKeyBase64String);
            return result;
        }

        Task<HttpResponseMessage> PostAsync(IReadOnlyDictionary<string, string> dic)
        {
            var url = GlobalAddress + "/s/api/game/user/token/check";
            var idic = DictionaryPool<string, string>.Shared.Get();
            using var dw = DisposeHelper.Create(c => DictionaryPool<string, string>.Shared.Return(c), idic);

            OwHelper.Copy(dic, idic);
            idic["sign"] = GetSignString(dic);

            var content = new FormUrlEncodedContent(new Dictionary<string, string>(idic)) { };
            return _HttpClient.PostAsync(url, content);
        }

        public void Login(T89LoginData datas)
        {
            var dic = DictionaryPool<string, string>.Shared.Get();
            using var dw = DisposeHelper.Create(c => DictionaryPool<string, string>.Shared.Return(c), dic);

            dic["appId"] = datas.AppId;
            dic["osType"] = datas.OsType.ToString();
            dic["serverId"] = datas.ServerId;
            dic["t"] = datas.T.ToString();
            dic["token"] = datas.Token;
            dic["uid"] = datas.Uid;

            var responese = PostAsync(dic).Result;
            if (!responese.IsSuccessStatusCode)
            {
                datas.ErrorCode = ErrorCodes.ERROR_INVALID_DATA;
                datas.ErrorMessage = responese.Content.ReadAsStringAsync().Result;
                return;
            }
            var responesStr = responese.Content.ReadAsStringAsync().Result;
            var re = (T89LoginReturn)JsonSerializer.Deserialize(responesStr, typeof(T89LoginReturn));
            if (re.Code != 0)
            {
                datas.ErrorCode = ErrorCodes.ERROR_INVALID_DATA;
                datas.ErrorMessage = responese.Content.ReadAsStringAsync().Result;
                return;
            }
        }
    }

    public class T89LoginReturn
    {
        [JsonPropertyName("code")]
        public int Code { get; set; }

        [JsonPropertyName("message")]
        public string Message { get; set; }

        [JsonPropertyName("messageType")]
        public string MessageType { get; set; }

        [JsonPropertyName("traceId")]
        public string TraceId { get; set; }
    }

    public class T89LoginData : GameContextBase, IResultWorkData
    {
        public T89LoginData([NotNull] VWorld world) : base(world)
        {
        }

        public T89LoginData([NotNull] IServiceProvider service) : base(service)
        {
        }

        #region IResultWorkData接口相关

        private bool? _HasError;

        /// <summary>
        /// 是否有错误。不设置则使用<see cref="ErrorCode"/>来判定。
        /// </summary>
        public bool HasError { get => _HasError ??= ErrorCode != ErrorCodes.NO_ERROR; set => _HasError = value; }

        /// <summary>
        /// 错误码，参见 ErrorCodes。
        /// </summary>
        public int ErrorCode { get; set; }

        /// <summary>
        /// 调试用的提示性信息。
        /// </summary>
        private string _ErrorMessage;

        /// <summary>
        /// 调试信息，如果发生错误，这里给出简要说明。
        /// </summary>
        public string ErrorMessage
        {
            get => _ErrorMessage ??= new Win32Exception(ErrorCode).Message;
            set => _ErrorMessage = value;
        }

        #endregion IResultWorkData接口相关

        public string AppId { get; set; }

        public long T { get; set; } = DateTime.UtcNow.Millisecond;

        public string Token { get; set; }

        public string Uid { get; set; }

        public string Sign { get; set; }

        public string ServerId { get; set; }

        public long OsType { get; set; }

        /// <summary>
        /// 密码。若首次登录，创建了账号则这里返回密码。否则返回null。
        /// </summary>
        public string Pwd { get; set; }

        /// <summary>
        /// 返回内部使用的令牌。
        /// </summary>
        public Guid InnerToken { get; set; }

        /// <summary>
        /// 成功则返回角色信息。
        /// </summary>
        public List<GameChar> GameChars { get; set; } = new List<GameChar>();

        /// <summary>
        /// 登录名。
        /// </summary>
        public string LoginName { get; set; }
    }

    public static class PublisherT89Extensions
    {
        public static IHttpClientBuilder AddPublisherT89(this IServiceCollection services)
        {
            return services.AddHttpClient<PublisherT89, PublisherT89>().SetHandlerLifetime(TimeSpan.FromMinutes(5)).ConfigureHttpClient(c =>
            {
                c.DefaultRequestHeaders.Add("ContentType", "application/x-www-form-urlencoded");
            });
        }
    }

    /// <summary>
    /// 样例代码类。
    /// </summary>
    class SHA1WithRSA
    {
        public static string Sign(string content, string privateKey)
        {
            byte[] Data = Encoding.GetEncoding("utf-8").GetBytes(content);
            RSA rsa = DecodePemPrivateKey(privateKey);
            byte[] signData = rsa.SignData(Data, HashAlgorithmName.SHA1, RSASignaturePadding.Pkcs1);
            return Convert.ToBase64String(signData);

        }

        public static bool Verify(string content, string signedString, string publicKey)
        {
            bool result = false;
            byte[] Data = Encoding.GetEncoding("utf-8").GetBytes(content);
            byte[] data = Convert.FromBase64String(signedString);
            RSAParameters paraPub = ConvertFromPublicKey(publicKey);
            RSACryptoServiceProvider rsaPub = new RSACryptoServiceProvider();
            rsaPub.ImportParameters(paraPub);
            using (var sh = SHA1.Create())
            {
                result = rsaPub.VerifyData(Data, sh, data);
                return result;
            }
        }

        private static RSA DecodePemPrivateKey(string pemstr)
        {
            RSA rsa = DecodeRSAPrivateKey(Convert.FromBase64String(pemstr));
            return rsa;
        }

        private static RSA DecodeRSAPrivateKey(byte[] privkey)
        {
            // --------- Set up stream to decode the asn.1 encoded RSA private key ------
            using MemoryStream mem = new MemoryStream(privkey);
            try
            {
                // ------- create RSACryptoServiceProvider instance and initialize with public key -----
                var rsa = RSA.Create();
                rsa.ImportPkcs8PrivateKey(privkey, out _);
                return rsa;
            }
            catch (Exception e)
            {
                return null;
            }
        }

        private static RSAParameters ConvertFromPublicKey(string pemFileConent)
        {
            if (string.IsNullOrEmpty(pemFileConent))
            {
                throw new ArgumentNullException("pemFileConent", "This arg cann't be empty.");
            }
            pemFileConent = pemFileConent.Replace("-----BEGIN PUBLIC KEY-----", "").Replace("-----END PUBLIC KEY-----", "").Replace("\n", "").Replace("\r", "");
            byte[] keyData = Convert.FromBase64String(pemFileConent);
            bool keySize1024 = (keyData.Length == 162);
            bool keySize2048 = (keyData.Length == 294);
            if (!(keySize1024 || keySize2048))
            {
                throw new ArgumentException("pem file content is incorrect, Only support the key size is 1024 or 2048");
            }
            byte[] pemModulus = (keySize1024 ? new byte[128] : new byte[256]);
            byte[] pemPublicExponent = new byte[3];
            Array.Copy(keyData, (keySize1024 ? 29 : 33), pemModulus, 0, (keySize1024 ? 128 : 256));
            Array.Copy(keyData, (keySize1024 ? 159 : 291), pemPublicExponent, 0, 3);
            RSAParameters para = new RSAParameters();
            para.Modulus = pemModulus;
            para.Exponent = pemPublicExponent;
            return para;
        }

    }
}
