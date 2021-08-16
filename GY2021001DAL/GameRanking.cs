using OW.Game.Store;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace GuangYuan.GY001.UserDb
{
    /// <summary>
    /// 排行榜单类。
    /// 记录了角色的各种排行积分。
    /// 此类Id就是所属对象的ID,故而需要不断添加属性展平，避免出现多个Id重复的现象。
    /// 需要在数据库中不严格实时排名的所需数据，都在此表内收集缓存。
    /// </summary>
    public class GameRanking : SimpleExtendPropertyBase
    {
        public GameRanking()
        {
        }

        public GameRanking(Guid id) : base(id)
        {
        }

        /// <summary>
        /// PVP排行积分
        /// </summary>
        /// <value>每月清理归为1000</value>
        public int PvpScore { get; set; }

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
        /// 最后下线时间，空则表示，当前在线。
        /// </summary>
        public DateTime? LastLogout { get; set; }

        /// <summary>
        /// 展示动物的身体模板Id以逗号分隔的数组。
        /// </summary>
        /// <value>例如：3ad139b7-9541-43ac-93cd-2aa2c4b48a45,0012f05f-de37-4c68-b216-1f0fcec0906a,</value>
        [MaxLength(192)]
        public string HomelandShow { get; set; }
    }
}
