﻿using OwGame;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace GY2021001DAL
{
    /// <summary>
    /// 游戏中物品，装备，货币，积分的基类。
    /// </summary>
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

    }

    /// <summary>
    /// 记录虚拟物品、资源变化的类。
    /// </summary>
    [NotMapped]
    public class ChangesItem
    {
        public ChangesItem()
        {

        }

        /// <summary>
        /// 容器的Id。如果是容器本身属性变化，这个成员是容器的上层容器Id,例如背包的容量变化了则这个成员就是角色Id。
        /// </summary>
        public DateTime DateTimeUtc { get; set; }

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
