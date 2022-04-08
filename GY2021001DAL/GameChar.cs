﻿using GuangYuan.GY001.TemplateDb;
using Microsoft.EntityFrameworkCore;
using OW.Game;
using OW.Game.Store;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
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
        Robot = 1,

        /// <summary>
        /// Npc有些轻度游戏中没有npc。
        /// </summary>
        Npc = 2,

        /// <summary>
        /// 开发时的测试账号。
        /// </summary>
        Test = 8,

        /// <summary>
        /// 特殊的贵宾角色。
        /// </summary>
        Vip = 4,

        /// <summary>
        /// 有管理员权力的角色。一般是运营人员。
        /// </summary>
        Admin = 16,

        /// <summary>
        /// 超管，一般是开发团队人员。
        /// </summary>
        SuperAdmin = 32,
    }

    public class CharBinaryExProperties
    {
        public Dictionary<string, string> ClientProperties { get; set; } = new Dictionary<string, string>();
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

        CharBinaryExProperties _BinaryExProperties;
        /// <summary>
        /// 用二进制根式存储的数据。
        /// </summary>
        [NotMapped]
        [JsonIgnore]
        public CharBinaryExProperties BinaryExProperties
        {
            get
            {
                if (_BinaryExProperties is null)
                {
                    if (BinaryArray is null || BinaryArray.Length <= 0)
                        _BinaryExProperties = new CharBinaryExProperties();
                    else
                        _BinaryExProperties = JsonSerializer.Deserialize(BinaryArray, typeof(CharBinaryExProperties)) as CharBinaryExProperties;
                }
                return _BinaryExProperties;
            }
        }


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
        /// <inheritdoc/>
        /// </summary>
        /// <param name="db"></param>
        public override void PrepareSaving(DbContext db)
        {
            if (_ChangesItems != null)    //若需要序列化变化属性
            {
                var exProp = ExtendProperties.FirstOrDefault(c => c.Name == ChangesItemExPropertyName);
                if (exProp is null)
                    exProp = new GameExtendProperty();
                exProp.Text = JsonSerializer.Serialize(_ChangesItems.Select(c => (ChangesItemSummary)c).ToList());
            }
            if (_BinaryExProperties != null)
            {
                BinaryArray = JsonSerializer.SerializeToUtf8Bytes(_BinaryExProperties, typeof(CharBinaryExProperties));
            }
            base.PrepareSaving(db);
        }

        private List<ChangeItem> _ChangesItems = new List<ChangeItem>();

        /// <summary>
        /// 保存未能发送给客户端的变化数据。
        /// </summary>
        [JsonIgnore]
        public List<ChangeItem> ChangesItems => _ChangesItems;

        /// <summary>
        /// 未发送给客户端的数据保存在<see cref="GameThingBase.ExtendProperties"/>中使用的属性名称。
        /// </summary>
        public const string ChangesItemExPropertyName = "{BAD410C8-6393-44B4-9EB1-97F91ED11C12}";

        /// <summary>
        /// 用户的类型。
        /// </summary>
        public CharType CharType { get; set; }

        /// <summary>
        /// 客户端的属性。
        /// </summary>
        [NotMapped]
        [JsonIgnore]
        public Dictionary<string, string> ClientProperties
        {
            get
            {
                return BinaryExProperties.ClientProperties;
            }
        }

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
                _ChangesItems = null;
                GameUser = null;
                _BinaryExProperties = null;
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

    [DataContract]
    public class ChangesItemSummary
    {
        /// <summary>
        /// 转换为摘要类。
        /// </summary>
        /// <param name="obj"></param>
        public static explicit operator ChangesItemSummary(ChangeItem obj)
        {
            var result = new ChangesItemSummary()
            {
                ContainerId = obj.ContainerId,
                DateTimeUtc = obj.DateTimeUtc,
            };
            result.AddIds.AddRange(obj.Adds.Select(c => c.Id));
            result.RemoveIds.AddRange(obj.Removes);
            result.ChangeIds.AddRange(obj.Changes.Select(c => c.Id));
            return result;
        }

        /// <summary>
        /// 从摘要类恢复完整对象。
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="gameChar"></param>
        /// <returns></returns>
        public static List<ChangeItem> ToChangesItem(IEnumerable<ChangesItemSummary> objs, GameChar gameChar)
        {
            var results = new List<ChangeItem>();
            var dic = gameChar.AllChildren.ToDictionary(c => c.Id);
            foreach (var obj in objs)
            {
                var result = new ChangeItem()
                {
                    ContainerId = obj.ContainerId,
                    DateTimeUtc = obj.DateTimeUtc
                };
                result.Adds.AddRange(obj.AddIds.Select(c => dic.GetValueOrDefault(c)).Where(c => c != null));
                result.Changes.AddRange(obj.ChangeIds.Select(c => dic.GetValueOrDefault(c)).Where(c => c != null));
                result.Removes.AddRange(obj.AddIds);
                results.Add(result);
            }
            return results;
        }

        [DataMember]
        public Guid ContainerId { get; set; }

        [DataMember]
        public List<Guid> AddIds { get; set; } = new List<Guid>();

        [DataMember]
        public List<Guid> RemoveIds { get; set; } = new List<Guid>();

        [DataMember]
        public List<Guid> ChangeIds { get; set; } = new List<Guid>();

        [DataMember]
        public DateTime DateTimeUtc { get; set; }
    }

    /// <summary>
    /// 
    /// </summary>
    public abstract class GameCharBase : GameThingBase, IDisposable
    {

        protected GameCharBase()
        {
        }

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
