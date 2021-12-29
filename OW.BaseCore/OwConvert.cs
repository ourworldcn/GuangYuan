/*
 * 文件放置游戏专用的一些基础类
 */
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;

namespace System
{
    /// <summary>
    /// 额外的转换函数汇总类。
    /// </summary>
    public static class OwConvert
    {
        #region 试图转换类型

        /// <summary>
        /// 试图把对象转换为数值。
        /// </summary>
        /// <param name="obj">null导致立即返回false。</param>
        /// <param name="result"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public static bool TryGetDecimal(object obj, out decimal result)
        {
            if (obj is null)
            {
                result = default;
                return false;
            }
            bool succ;
            switch (Type.GetTypeCode(obj.GetType()))
            {
                case TypeCode.SByte:
                case TypeCode.Byte:
                case TypeCode.Int16:
                case TypeCode.UInt16:
                case TypeCode.Int32:
                case TypeCode.UInt32:
                case TypeCode.Int64:
                case TypeCode.UInt64:
                case TypeCode.Single:
                case TypeCode.Double:
                    result = Convert.ToDecimal(obj);
                    succ = true;
                    break;
                case TypeCode.Decimal:
                    result = (decimal)obj;
                    succ = true;
                    break;
                case TypeCode.String:
                    succ = decimal.TryParse(obj as string, out result);
                    break;
                case TypeCode.Char:
                case TypeCode.DateTime:
                case TypeCode.Empty:
                case TypeCode.DBNull:
                case TypeCode.Boolean:
                default:
                    result = decimal.Zero;
                    succ = false;
                    break;
                case TypeCode.Object:
                    if (obj is JsonElement json)
                    {
                        succ = json.TryGetDecimal(out result);
                    }
                    else
                    {
                        result = decimal.Zero;
                        succ = false;
                    }
                    break;
            }
            return succ;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="dic"></param>
        /// <param name="key"></param>
        /// <param name="defaultVal"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public static bool TryGetFloat(object obj, out float result)
        {
            if (obj is null)
            {
                result = default;
                return false;
            }
            bool succ;
            switch (Type.GetTypeCode(obj.GetType()))
            {
                case TypeCode.Boolean:
                case TypeCode.SByte:
                case TypeCode.Byte:
                case TypeCode.Int16:
                case TypeCode.UInt16:
                case TypeCode.Int32:
                case TypeCode.UInt32:
                case TypeCode.Int64:
                case TypeCode.UInt64:
                case TypeCode.Single:
                case TypeCode.Double:
                case TypeCode.Decimal:
                    result = Convert.ToSingle(obj);
                    succ = true;
                    break;
                case TypeCode.String:
                    succ = float.TryParse(obj as string, out result);
                    break;
                case TypeCode.Object:
                case TypeCode.Empty:
                case TypeCode.DBNull:
                case TypeCode.Char:
                case TypeCode.DateTime:
                default:
                    result = default;
                    succ = false;
                    break;
            }
            return succ;
        }

        /// <summary>
        /// 尽可能转换为Guid类型。
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="result"></param>
        /// <returns>true成功转换，false未成功。</returns>
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public static bool TryGetGuid(object obj, out Guid result)
        {

            if (obj is Guid id)
            {
                result = id;
                return true;
            }
            else if (obj is string str)
            {
                if (str.EndsWith("=="))
                {
                    result = ToGuid(str);
                    return true;
                }
                else if (Guid.TryParse(str, out result))
                    return true;
            }
            if (obj is byte[] ary && ary.Length == 16)
            {
                try
                {
                    result = new Guid(ary);

                }
                catch (Exception)
                {
                    result = default;
                    return false;
                }
                return true;
            }
            else
            {
                result = default;
                return false;
            }
        }

        /// <summary>
        /// 尽可能转换为Guid类型。
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public static bool TryGetBoolean(object obj, out bool result)
        {
            if (obj is bool b)
            {
                result = b;
                return true;
            }
            else if (obj is string str && bool.TryParse(str, out result))
                return true;
            else if (TryGetDecimal(obj, out var deci))
            {
                result = deci != decimal.Zero;
                return true;
            }
            result = false;
            return false;
        }
        #endregion 试图转换类型

        /// <summary>
        /// 从属性字典获取字符串表现形式。
        /// </summary>
        /// <param name="dic"></param>
        /// <returns></returns>
        public static string ToString(IReadOnlyDictionary<string, object> dic)
        {
            StringBuilder sb = new StringBuilder();
            OwHelper.Fill(dic, sb);
            return sb.ToString();
        }

        /// <summary>
        /// 将字符串转换为Guid类型。
        /// </summary>
        /// <param name="str">可以是<see cref="Guid.TryParse(string?, out Guid)"/>接受的格式，
        /// 也可以是Base64表示的内存数组模式，即<see cref="Guid.ToByteArray"/>的Base64编码模式。
        /// 对于空和空字符串会返回<see cref="Guid.Empty"/></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Guid ToGuid(string str)
        {
            if (string.IsNullOrEmpty(str))
                return Guid.Empty;
            if (Guid.TryParse(str, out var result))
                return result;
            Span<byte> span = stackalloc byte[16];
            if (!Convert.TryFromBase64String(str, span, out var lengthe))
                throw new FormatException($"不是有效的数据格式——{str}");
            return new Guid(span);
        }

        /// <summary>
        /// 用Base64编码Guid类型。
        /// </summary>
        /// <param name="guid"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string ToBase64String(this Guid guid)
        {
            Span<byte> span = stackalloc byte[16];
            guid.TryWriteBytes(span);
            return Convert.ToBase64String(span);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public static bool TryGetDateTime(object obj, out DateTime result)
        {
            if (obj is DateTime dt)
            {
                result = dt;
                return true;
            }
            if (obj is string str && DateTime.TryParse(str, out result))
                return true;
            result = default;
            return false;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="obj">null,return <see cref="string.Empty"/></param>
        /// <returns></returns>
        public static string ToUriString<T>(T obj) where T : new()
        {
            if (obj is null)
                return string.Empty;
            return Uri.EscapeDataString(JsonSerializer.Serialize(obj, typeof(T)));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="str"><see cref="string.IsNullOrWhiteSpace(string?)"/>return true,return new T()</param>
        /// <returns></returns>
        public static T FromUriString<T>(string str) where T : new()
        {
            if (string.IsNullOrWhiteSpace(str))
                return new T();
            return (T)JsonSerializer.Deserialize(Uri.UnescapeDataString(str), typeof(T));
        }

        /// <summary>
        /// 复制字典中的内容。
        /// </summary>
        /// <typeparam name="TKey"></typeparam>
        /// <typeparam name="TValue"></typeparam>
        /// <param name="src"></param>
        /// <param name="dest">如果源字典存在与目标字典中相同的键，则用源中数据覆盖目标字典中数据。</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public static void Fill<TKey, TValue>(IReadOnlyDictionary<TKey, TValue> src, IDictionary<TKey, TValue> dest)
        {
            foreach (var item in src)
                dest[item.Key] = item.Value;
        }
    }
}