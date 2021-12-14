using OW.Game.Store;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;
using System.Text.Json.Serialization;

namespace GuangYuan.GY001.UserDb
{
    /// <summary>
    /// 特定于项目的角色扩展信息类。根据具体需要建立索引。这可能是一个很大的表，且会不断增加字段。
    /// 它记录一些强类型的信息，便于查询。例如：角色的各种排行积分。
    /// 此类Id就是所属对象的ID,故而需要不断添加属性展平，避免出现多个Id重复的现象。
    /// 需要在数据库中不严格实时排名的所需数据，都在此表内收集缓存。
    /// </summary>
    /// <remarks>写入此对象，需要锁定相应的角色对象。读取可以幻读。
    /// </remarks>
    public class CharSpecificExpandProperty : SimpleDynamicPropertyBase
    {
        //
        public CharSpecificExpandProperty()
        {
        }

        public CharSpecificExpandProperty(Guid id) : base(id)
        {
        }

        /// <summary>
        /// 角色对象的导航属性。
        /// </summary>
        [ForeignKey(nameof(Id))]
        [JsonIgnore]
        public virtual GameChar GameChar { get; set; }

        /// <summary>
        /// 角色等级。
        /// </summary>
        public int CharLevel { get; set; }

        /// <summary>
        /// PVP排行积分
        /// </summary>
        /// <value>每月清理归为1000</value>
        public int PvpScore { get; set; }

        /// <summary>
        /// 上一赛季结算时的积分。
        /// </summary>
        public int LastPvpScore { get; set; }

        /// <summary>
        /// 塔防积分，当前保留未用。
        /// </summary>
        public int PveTScore
        {
            get;
            set;
        }

        /// <summary>
        /// Pve捕获积分。
        /// </summary>
        public int PveCScore
        {
            get;
            set;
        }

        /// <summary>
        /// 记录一些附属信息。
        /// 这个字段记录短信息，可以使用索引加速查找。
        /// </summary>
        [MaxLength(64)]
        public string State
        {
            get;
            set;
        }

        /// <summary>
        /// 最后下线的Utc时间。
        /// 为了便于查询，在线时该属性标记为 9999-1-1 0:0:0，刚刚创建的用户是创建时间。
        /// </summary>
        public DateTime LastLogoutUtc { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// 好友位最大数。
        /// </summary>
        public int FrinedMaxCount { get; set; }

        /// <summary>
        /// 当前好友数。
        /// </summary>
        public int FrinedCount { get; set; }
    }
}
