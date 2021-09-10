/*
 * 文件放置游戏专用的一些基础类
 */
using System;
using System.Runtime.CompilerServices;

namespace OW.Game
{
    public static class GameHelper
    {
        /// <summary>
        /// 用Base64编码Guid类型。
        /// </summary>
        /// <param name="guid"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string ToBase64String(this Guid guid)
        {
            return Convert.ToBase64String(guid.ToByteArray());
        }

        /// <summary>
        /// 从Base64编码转换获取Guid值。
        /// </summary>
        /// <param name="str">空引用，空字符串，空白导致返回<see cref="Guid.Empty"/></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Guid FromBase64String(string str)
        {
            return string.IsNullOrWhiteSpace(str) ? Guid.Empty : new Guid(Convert.FromBase64String(str));
        }

    }
}