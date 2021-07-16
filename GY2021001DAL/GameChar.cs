﻿using Microsoft.EntityFrameworkCore;
using OwGame;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace GY2021001DAL
{
    public class GameChar : GameThingBase, IDisposable
    {

        public GameChar()
        {
            Id = Guid.NewGuid();
        }

        public GameChar(Guid id)
        {
            Id = id;
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
        /// 一个角色初始创建时被调用。
        /// 通常这里预制一些道具，装备。
        /// </summary>
        public void InitialCreation()
        {
            foreach (var item in GameItems)
                item.GameChar = this;
        }

        List<GameItem> _GameItems;

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
                    _GameItems = GameUser.DbContext.Set<GameItem>().Where(c => c.OwnerId == Id).Include(c => c.Children).ThenInclude(c => c.Children).ToList();
                }
                return _GameItems;
            }
            internal set
            {
                _GameItems = value;
            }
        }

        /// <summary>
        /// 获取该物品直接或间接下属对象的枚举数。深度优先。
        /// </summary>
        /// <returns>枚举数。不包含自己。枚举过程中不能更改树节点的关系。</returns>
        [NotMapped]
        public IEnumerable<GameItem> AllChildren
        {
            get
            {
                foreach (var item in GameItems)
                {
                    yield return item;
                    foreach (var item2 in item.AllChildren)
                        yield return item2;
                }
            }
        }

        /// <summary>
        /// 获取该物品直接或间接下属对象的枚举数。广度优先。
        /// </summary>
        /// <returns>枚举数。不包含自己。枚举过程中不能更改树节点的关系。</returns>
        [NotMapped]
        public IEnumerable<GameItem> AllChildrenWithBfs
        {
            get => OwHelper.GetAllSubItemsOfTreeWithBfs(c => c.Children, GameItems.ToArray());
        }

        /// <summary>
        /// 所属用户Id。
        /// </summary>
        [ForeignKey(nameof(GameUser))]
        public Guid GameUserId { get; set; }

        /// <summary>
        /// 所属用户的导航属性。
        /// </summary>
        public virtual GameUser GameUser { get; set; }

        /// <summary>
        /// 角色显示用的名字。
        /// </summary>
        public string DisplayName { get; set; }

        /// <summary>
        /// 用户所处地图区域的Id,这也可能是战斗关卡的Id。如果没有在战斗场景中，则可能是空。
        /// </summary>
        public Guid? CurrentDungeonId { get; set; }

        /// <summary>
        /// 进入战斗场景的时间。注意是Utc时间。如果没有在战斗场景中，则可能是空。
        /// </summary>
        public DateTime? CombatStartUtc { get; set; }

        Dictionary<string, FastChangingProperty> _GradientProperties;

        /// <summary>
        /// 渐变属性字典。
        /// </summary>
        [NotMapped]
        public IReadOnlyDictionary<string, FastChangingProperty> GradientProperties
        {
            get
            {
                if (null == _GradientProperties)
                {
                    _GradientProperties = new Dictionary<string, FastChangingProperty>()
                    {
                        {
                            "pp",
                            new FastChangingProperty(TimeSpan.FromSeconds(Convert.ToDouble( Properties.GetValueOrDefault("dpp",300m))),(decimal)Properties.GetValueOrDefault("ipp",1m),
                                (decimal)Properties.GetValueOrDefault("mpp",20m),(decimal)Properties.GetValueOrDefault("pp",20m),DateTime.Parse( Properties.GetValueOrDefault("cpp",DateTime.UtcNow.ToString()) as string))
                            { Name="pp"}
                        },
                    };
                }
                return _GradientProperties;
            }
        }


        private Dictionary<string, GameClientExtendProperty> _ClientExtendProperties = new Dictionary<string, GameClientExtendProperty>();

        /// <summary>
        /// 客户端使用的扩展属性集合，服务器不使用该属性，仅帮助保存和传回。
        /// 键最长64字符，值最长8000字符。（一个中文算一个字符）
        /// </summary>
        [NotMapped]
        public IDictionary<string, GameClientExtendProperty> ClientExtendProperties { get => _ClientExtendProperties; }

        /// <summary>
        /// 在基础数据加载到内存后调用。
        /// </summary>
        public void InvokeLoaded()
        {
            var db = GameUser.DbContext;
            //加载所属物品对象
            _GameItems ??= db.Set<GameItem>().Where(c => c.OwnerId == Id).Include(c => c.Children).ThenInclude(c => c.Children).ThenInclude(c => c.Children).Include(c => c.ExtendProperties).ToList();
            foreach (var item in _GameItems)
            {
                item.GameChar = this;
            }
            //加载客户端属性
            var coll = db.Set<GameClientExtendProperty>().Where(c => c.ParentId == Id);
            foreach (var item in coll)
            {
                ClientExtendProperties[item.Name] = item;
            }
            foreach (var item in AllChildren)
            {

            }
            var exProp = ExtendProperties.FirstOrDefault(c => c.Name == ChangesItemExPropertyName);
            if (null != exProp)    //若有需要反序列化的对象
            {
                var tmp = JsonSerializer.Deserialize<List<ChangesItemSummary>>(exProp.Text);
                _ChangesItems = ChangesItemSummary.ToChangesItem(tmp, this);
            }
        }

        override protected void OnSaving(EventArgs e)
        {
            if (null != _GradientProperties)   //若已经生成了渐变属性
            {
                DateTime dtNow = DateTime.UtcNow;
                if (_GradientProperties.TryGetValue("pp", out FastChangingProperty p))
                {
                    Properties["pp"] = p.GetCurrentValue(ref dtNow);
                    Properties["cpp"] = p.LastDateTime.ToString();
                }
            }
            foreach (var item in OwHelper.GetAllSubItemsOfTree(GameItems, c => c.Children).ToArray())
                item.InvokeSaving(EventArgs.Empty);
            if (_ChangesItems != null)    //若需要序列化变化属性
            {
                var exProp = ExtendProperties.FirstOrDefault(c => c.Name == ChangesItemExPropertyName);
                if (exProp is null)
                    exProp = new GameExtendProperty();
                exProp.Text = JsonSerializer.Serialize(_ChangesItems.Select(c => (ChangesItemSummary)c).ToList());
            }
            base.OnSaving(e);
        }

        private List<ChangesItem> _ChangesItems;
        private bool disposedValue;

        /// <summary>
        /// 保存未能发送给客户端的变化数据。
        /// </summary>
        public List<ChangesItem> ChangesItems => _ChangesItems ??= new List<ChangesItem>();

        /// <summary>
        /// 未发送给客户端的数据保存在<see cref="GameThingBase.ExtendProperties"/>中使用的属性名称。
        /// </summary>
        public const string ChangesItemExPropertyName = "{BAD410C8-6393-44B4-9EB1-97F91ED11C12}";

        #region IDisposable接口相关

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: 释放托管状态(托管对象)
                    if (!disposedValue)  //若第一次调用
                        OnDisposed(EventArgs.Empty);
                }

                // TODO: 释放未托管的资源(未托管的对象)并重写终结器
                // TODO: 将大型字段设置为 null
                disposedValue = true;
            }
        }

        virtual protected void OnDisposed(EventArgs e)
        {
            Disposed?.Invoke(this, e);
        }

        /// <summary>
        /// 对象已经被处置。
        /// </summary>
        public event EventHandler Disposed;

        // // TODO: 仅当“Dispose(bool disposing)”拥有用于释放未托管资源的代码时才替代终结器
        // ~GameChar()
        // {
        //     // 不要更改此代码。请将清理代码放入“Dispose(bool disposing)”方法中
        //     Dispose(disposing: false);
        // }

        public void Dispose()
        {
            // 不要更改此代码。请将清理代码放入“Dispose(bool disposing)”方法中
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        #endregion IDisposable接口相关
    }

    [DataContract]
    public class ChangesItemSummary
    {
        /// <summary>
        /// 转换为摘要类。
        /// </summary>
        /// <param name="obj"></param>
        public static explicit operator ChangesItemSummary(ChangesItem obj)
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
        public static List<ChangesItem> ToChangesItem(IEnumerable<ChangesItemSummary> objs, GameChar gameChar)
        {
            var results = new List<ChangesItem>();
            var dic = gameChar.AllChildren.ToDictionary(c => c.Id);
            foreach (var obj in objs)
            {
                var result = new ChangesItem()
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
}
