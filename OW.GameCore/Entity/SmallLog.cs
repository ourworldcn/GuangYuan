
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace OW.Game.Entity.Log
{
    /// <summary>
    /// 记录一些小型数据可表述的操作的数据结构类。
    /// </summary>
    public class SmallGameLogEntity<TParams>
    {
        /// <summary>
        /// 构造函数。
        /// </summary>
        public SmallGameLogEntity()
        {
        }

        public SmallGameLogEntity(IEnumerable<TParams> list)
        {
            Params.AddRange(list);
        }

        /// <summary>
        /// 发生的时间点。
        /// </summary>
        public DateTime DateTime { get; set; }

        /// <summary>
        /// 操作标识。
        /// </summary>
        public string ActionId { get; set; }

        /// <summary>
        /// 操作的相关的参数或结果记录。
        /// </summary>
        public List<TParams> Params { get; set;/*要Json序列化在默认情况下可反序列化则需要设置器*/ } = new List<TParams>();

    }

    /// <summary>
    /// 用于记录一些操作结果的日志，可能影响到后续的操作条件，如购买物品在周期内的限定。
    /// </summary>
    public class SmallGameLogEntityCollection<TParams> : Collection<SmallGameLogEntity<TParams>>
    {
        /// <summary>
        /// 构造函数。
        /// </summary>
        public SmallGameLogEntityCollection()
        {
        }

        public SmallGameLogEntityCollection(IEnumerable<SmallGameLogEntity<TParams>> list)
        {
            OwHelper.Copy(list, this);
        }

        public SmallGameLogEntity<TParams> GetOrAdd(string actionId)
        {
            var result = this.FirstOrDefault(c => c.ActionId == actionId);
            if (result is null)
            {
                result = new SmallGameLogEntity<TParams>() { ActionId = actionId };
                Add(result);
            }
            return result;
        }
    }

    /// <summary>
    /// 专门储存当日数据和当次数据的结构。
    /// </summary>
    /// <typeparam name="TParams"></typeparam>
    public class TodayLogEntity<TParams> : SmallGameLogEntityCollection<TParams>
    {
        public TodayLogEntity()
        {

        }

        [JsonIgnore]
        public SmallGameLogEntity<TParams> Last { get => GetOrAdd(nameof(Last)); }

        [JsonIgnore]
        public SmallGameLogEntity<TParams> LastDay { get => GetOrAdd(nameof(LastDay)); }

        /// <summary>
        /// 移除指定日期之前的数据。并重置为指定时间。
        /// </summary>
        /// <param name="today">仅取日期部分。</param>
        /// <returns>true成功移除了数据，false,没有指定日期之前的数据。</returns>
        public bool RemoveOld(DateTime today)
        {
            bool result = false;
            var last = Last;
            if (last.DateTime.Date < today.Date)
            {
                last.Params.Clear();
                last.DateTime = today;
                result = true;
            }
            var lastDay = LastDay;
            if (lastDay.DateTime.Date < today.Date)
            {
                lastDay.Params.Clear();
                lastDay.DateTime = today;
                result = true;
            }
            return result;
        }
    }
}