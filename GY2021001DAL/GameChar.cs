using GuangYuan.GY001.TemplateDb;
using Microsoft.EntityFrameworkCore;
using OW.Game;
using OW.Game.Store;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Serialization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace GuangYuan.GY001.UserDb
{
    /// <summary>
    /// 角色的类型。
    /// </summary>
    [Flags]
    public enum CharType : byte
    {
        /// <summary>
        /// 普通角色。
        /// </summary>
        Unknow = 0,

        /// <summary>
        /// 机器人。
        /// </summary>
        [Description("机器人")]
        Robot = 1,

        /// <summary>
        /// Npc有些轻度游戏中没有npc。
        /// </summary>
        [Description("Npc")]
        Npc = 2,

        /// <summary>
        /// 开发时的测试账号。
        /// </summary>
        [Description("测试账号")]
        Test = 8,

        /// <summary>
        /// 特殊的贵宾角色。
        /// </summary>
        [Description("VIP账号")]
        Vip = 4,

        /// <summary>
        /// 有管理员权力的角色。一般是运营人员。
        /// </summary>
        [Description("运营账号")]
        Admin = 16,

        /// <summary>
        /// 超管，一般是开发团队人员。
        /// </summary>
        [Description("超管账号")]
        SuperAdmin = 32,
    }

    [Table("GameChars")]
    public class GameChar : GameCharBase, IDisposable
    {
        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public GameChar()
        {
            Id = Guid.NewGuid();
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <param name="id"><inheritdoc/></param>
        public GameChar(Guid id) : base(id)
        {
        }

        /// <summary>
        /// 构造函数。用于延迟加载。
        /// </summary>
        /// <param name="lazyLoader">延迟加载器。</param>
        //private GameChar(Action<object, string> lazyLoader)
        //{
        //    LazyLoader = lazyLoader;
        //}

        //public Action<object,string> LazyLoader { get; set; }

        /// <summary>
        /// 创建该对象的通用协调时间。
        /// </summary>
        public DateTime CreateUtc { get; set; } = DateTime.UtcNow;

        private List<GameItem> _GameItems;

        /// <summary>
        /// 直接拥有的事物。
        /// 通常是一些容器，但也有个别不是。
        /// </summary>
        [NotMapped]
        public List<GameItem> GameItems
        {
            get
            {
                if (null == _GameItems)
                {
                    _GameItems = GetDbContext().Set<GameItem>().Where(c => c.OwnerId == Id).Include(c => c.Children).ThenInclude(c => c.Children).ThenInclude(c => c.Children).ToList();
                    _GameItems.ForEach(c => c.SetGameChar(this));
                }
                return _GameItems;
            }
            set
            {
                _GameItems = value;
            }
        }

        /// <summary>
        /// 获取该物品直接或间接下属对象的枚举数。广度优先。
        /// </summary>
        /// <returns>枚举数。不包含自己。枚举过程中不能更改树节点的关系。</returns>
        [NotMapped]
        [JsonIgnore]
        public IEnumerable<GameItem> AllChildren
        {
            get
            {
                foreach (var item in GameItems)
                    yield return item;
                foreach (var item in GameItems)
                    foreach (var item2 in item.GetAllChildren())
                        yield return item2;
            }
        }

        /// <summary>
        /// 所属用户Id。
        /// </summary>
        [ForeignKey(nameof(GameUser))]
        public Guid GameUserId { get; set; }

        /// <summary>
        /// 所属用户的导航属性。
        /// </summary>
        [JsonIgnore]
        public virtual GameUser GameUser { get; set; }

        /// <summary>
        /// 角色显示用的名字。就是昵称，不可重复。
        /// </summary>
        [MaxLength(64)]
        public string DisplayName { get; set; }

        /// <summary>
        /// 用户所处地图区域的Id,这也可能是战斗关卡的Id。如果没有在战斗场景中，则可能是空。
        /// </summary>
        [JsonIgnore]
        public Guid? CurrentDungeonId { get; set; }

        /// <summary>
        /// 进入战斗场景的时间。注意是Utc时间。如果没有在战斗场景中，则可能是空。
        /// </summary>
        public DateTime? CombatStartUtc { get; set; }

        /// <summary>
        /// 用户的类型。
        /// </summary>
        public CharType CharType { get; set; }

        #region IDisposable接口相关

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <param name="disposing"></param>
        protected override void Dispose(bool disposing)
        {
            if (!IsDisposed)
            {
                if (disposing)
                {
                    // 释放托管状态(托管对象)
                    _GameItems?.ForEach(c => (c as IDisposable)?.Dispose());  //对独占拥有的子对象调用处置
                }
                // 释放未托管的资源(未托管的对象)并重写终结器
                // 将大型字段设置为 null
                _GameItems = null;
                GameUser = null;
                base.Dispose(disposing);
            }
        }

        /// <summary>
        /// 获取此对象所处的用户数据库上下文对象。
        /// </summary>
        public override DbContext GetDbContext()
        {
            return GameUser?.DbContext;
        }

        // 仅当“Dispose(bool disposing)”拥有用于释放未托管资源的代码时才替代终结器
        // ~GameChar()
        // {
        //     // 不要更改此代码。请将清理代码放入“Dispose(bool disposing)”方法中
        //     Dispose(disposing: false);
        // }

        #endregion IDisposable接口相关
    }

    /// <summary>
    /// 
    /// </summary>
    public abstract class GameCharBase : GameThingBase, IDisposable
    {
        /// <summary>
        /// 构造函数。
        /// </summary>
        protected GameCharBase()
        {
        }

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="id"></param>
        protected GameCharBase(Guid id) : base(id)
        {
        }

        protected override void Dispose(bool disposing)
        {
            if (!IsDisposed)
            {
                if (disposing)
                {
                    // 释放托管状态(托管对象)
                }

                // 释放未托管的资源(未托管的对象)并重写终结器
                // 将大型字段设置为 null
                base.Dispose(disposing);
            }
        }

        // 仅当“Dispose(bool disposing)”拥有用于释放未托管资源的代码时才替代终结器
        // ~GameCharBase()
        // {
        //     // 不要更改此代码。请将清理代码放入“Dispose(bool disposing)”方法中
        //     Dispose(disposing: false);
        // }
    }
}
