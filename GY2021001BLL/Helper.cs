using GY2021001DAL;
using Gy2021001Template;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GY2021001BLL
{
    public static class GameHelper
    {
        /// <summary>
        /// 用Base64编码Guid类型。
        /// </summary>
        /// <param name="guid"></param>
        /// <returns></returns>
        public static string ToBase64String(this Guid guid)
        {
            return Convert.ToBase64String(guid.ToByteArray());
        }

        /// <summary>
        /// 从Base64编码转换获取Guid值。
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static Guid FromBase64String(string str)
        {
            return new Guid(Convert.FromBase64String(str));
        }

        public static GameItemTemplate GetTemplate(this GameChar gameChar)
        {
            return null;
        }

    }
}

