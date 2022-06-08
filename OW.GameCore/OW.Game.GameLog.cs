﻿using GuangYuan.GY001.UserDb;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.Json;

namespace OW.Game.Log
{
    public class SimpleGameLog
    {
        /// <summary>
        /// 构造函数。
        /// </summary>
        public SimpleGameLog()
        {

        }

        /// <summary>
        /// 发生的时间点。
        /// </summary>
        public DateTime DateTime { get; set; }

        /// <summary>
        /// 操作。
        /// </summary>
        public string Action { get; set; }

        /// <summary>
        /// 操作的对象。
        /// </summary>
        public List<string> Params { get; set; } = new List<string>();

    }

    /// <summary>
    /// 用于记录一些操作结果的日志，可能影响到后续的操作条件，如购买物品在周期内的限定。
    /// </summary>
    public class SimpleGameLogCollection : Collection<SimpleGameLog>
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="str">空引用或空字符串将立即返回一个空对象。</param>
        /// <returns></returns>
        public static SimpleGameLogCollection Parse([AllowNull] string str)
        {
            if (string.IsNullOrWhiteSpace(str))
                return new SimpleGameLogCollection();
            return (SimpleGameLogCollection)JsonSerializer.Deserialize(Uri.UnescapeDataString(str), typeof(SimpleGameLogCollection));
        }
        public static SimpleGameLogCollection Parse(IDictionary<string, object> dic, string key)
        {
            if (!dic.TryGetValue(key, out var obj) || !(obj is string str))
                str = null;
            var result = Parse(str);
            result.Dictionary = dic;
            result.KeyName = key;
            return result;
        }

        /// <summary>
        /// 构造函数。
        /// </summary>
        public SimpleGameLogCollection()
        {
        }

        public IDictionary<string, object> Dictionary { get; set; }

        public string KeyName { get; set; }

        /// <summary>
        /// 保存到指定的字典数据中。
        /// </summary>
        /// <param name="dic"></param>
        /// <param name="kenName"></param>
        public void Save(IDictionary<string, object> dic, string kenName)
        {
            dic[kenName] = Uri.EscapeDataString(JsonSerializer.Serialize(this));
        }

        public void Save()
        {
            Dictionary[KeyName] = Uri.EscapeDataString(JsonSerializer.Serialize(this));
        }

        public SimpleGameLog Add(string action, Guid id, decimal count)
        {
            var tmp = new SimpleGameLog() { Action = action, DateTime = DateTime.UtcNow };
            tmp.Params.Add(id.ToString());
            tmp.Params.Add(count.ToString());
            Add(tmp);
            return tmp;
        }

        /// <summary>
        /// 移除小于指定时间的数据。
        /// </summary>
        /// <param name="end"></param>
        public void Remove(DateTime end)
        {
            for (int i = Count - 1; i >= 0; i--)
            {
                if (this[i].DateTime < end)
                    RemoveAt(i);
            }
        }

        public IEnumerable<SimpleGameLog> Get(DateTime today)
        {
            return this.Where(c => c.DateTime.Date == today.Date);
        }
    }
}
