using GuangYuan.GY001.UserDb;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace OW.Game
{
    [OwAutoInjection(ServiceLifetime.Scoped)]
    public class GameCommandManager
    {
        public GameCommandManager()
        {

        }

        /// <summary>
        /// 发送当前命令的角色。
        /// </summary>
        public GameChar CurrentChar { get; set; }

        string _Token;
        /// <summary>
        /// 设置令牌。
        /// </summary>
        /// <param name="token"></param>
        public void SetToken(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                throw new ArgumentException($"“{nameof(token)}”不能为 null 或空白。", nameof(token));
            }
            
            _Token = token;
        }

        public void Handle<T>(T command)
        {

        }
    }
}
