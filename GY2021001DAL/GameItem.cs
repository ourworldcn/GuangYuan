﻿using OwGame;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace GY2021001DAL
{
    /// <summary>
    /// 游戏中物品，装备，货币，积分的基类。
    /// </summary>
    [DebuggerDisplay("{" + nameof(GetDebuggerDisplay) + "(),nq}")]
    public class GameItem : GameThingBase
    {
        /// <summary>
        /// 构造函数。
        /// </summary>
        public GameItem()
        {

        }

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="id">指定Id。</param>
        public GameItem(Guid id) : base(id)
        {

        }

        /// <summary>
        /// 此物品的数量。
        /// 可能没有数量属性，如装备永远是1。对货币类(积分)都使用的是实际值。
        /// </summary>
        public decimal? Count { get; set; }

        /// <summary>
        /// 所属槽导航属性。
        /// </summary>
        public virtual GameItem Parent { get; set; }

        /// <summary>
        /// 所属槽Id。
        /// </summary>
        [ForeignKey(nameof(Parent))]
        public Guid? ParentId { get; set; }

        /// <summary>
        /// 拥有的槽。
        /// </summary>
        public virtual List<GameItem> Children { get; } = new List<GameItem>();

        /// <summary>
        /// 所属角色Id或其他关联对象的Id。
        /// </summary>
        public Guid? OwnerId { get; set; }

        private string GetDebuggerDisplay()
        {
            if (Properties.TryGetValue("tname", out object obj) && obj is string result)
                return result;
            return ToString();
        }
    }

    /// <summary>
    /// 记录虚拟物品、资源变化的类。
    /// </summary>
    [NotMapped]
    public class ChangesItem
    {
        /// <summary>
        /// 按容器Id化简集合。
        /// </summary>
        /// <param name="changes"></param>
        public static void Reduce(List<ChangesItem> changes)
        {
            var _ = from tmp in changes
                    group tmp by tmp.ContainerId into g
                    where g.Count() > 1
                    select (g.Key, g.First(), g.Skip(1).ToArray());
            var lst = _.ToList();
            foreach (var item in lst)
            {
                item.Item2.Adds.AddRange(item.Item3.SelectMany(c => c.Adds));
                item.Item2.Changes.AddRange(item.Item3.SelectMany(c => c.Changes));
                item.Item2.Removes.AddRange(item.Item3.SelectMany(c => c.Removes));
            }
            foreach (var item in lst.SelectMany(c => c.Item3))
            {
                changes.Remove(item);
            }
        }

        public ChangesItem()
        {

        }

        /// <summary>
        /// 容器的Id。如果是容器本身属性变化，这个成员是容器的上层容器Id,例如背包的容量变化了则这个成员就是角色Id。
        /// </summary>
        public DateTime DateTimeUtc { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// 容器的实例Id。
        /// </summary>
        public Guid ContainerId { get; set; }

        /// <summary>
        /// 增加的数据。
        /// </summary>
        public List<GameItem> Adds { get; } = new List<GameItem>();

        /// <summary>
        /// 删除的对象的唯一Id集合。
        /// </summary>
        public List<Guid> Removes { get; } = new List<Guid>();

        /// <summary>
        /// 变化的数据。
        /// </summary>
        public List<GameItem> Changes { get; } = new List<GameItem>();

    }

}
