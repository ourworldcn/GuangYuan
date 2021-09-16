using GuangYuan.GY001.TemplateDb;
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

namespace GuangYuan.GY001.UserDb
{
    [Table("GameChars")]
    public class GameChar : GameCharBase
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
        /// <param _Name="id"><inheritdoc/></param>
        public GameChar(Guid id) : base(id)
        {
        }

        /// <summary>
        /// 构造函数。用于延迟加载。
        /// </summary>
        /// <param _Name="lazyLoader">延迟加载器。</param>
        //private GameChar(Action<object, string> lazyLoader)
        //{
        //    LazyLoader = lazyLoader;
        //}

        //public Action<object,string> LazyLoader { get; set; }

        private List<GameItem> _GameItems;

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <param name="service"></param>
        /// <param name="parameters"><inheritdoc/>。另外额外需要user指定其所属的用户对象。</param>
        /// <exception cref="InvalidOperationException">缺少键值指定该对象所属的<see cref="GameUser"/>对象。</exception>
        protected override void InitializeCore(IServiceProvider service, IReadOnlyDictionary<string, object> parameters)
        {
            base.InitializeCore(service, parameters);
            //初始化本类型特殊数据
            if (parameters.TryGetValue("user", out var obj) && obj is GameUser gu)
            {
                GameUserId = gu.Id;
                GameUser = gu;
            }
            else
                throw new InvalidOperationException($"缺少键值指定该对象所属的{nameof(GameUser)}对象。");
            DisplayName = parameters.GetValueOrDefault(nameof(DisplayName)) as string;
            var db = DbContext;
            //追加子对象
            if (Template.ChildrenTemplateIds.Count > 0)
            {
                Dictionary<string, object> dic = new Dictionary<string, object>()
                {
                    {"owner",this },
                };
                _GameItems ??= new List<GameItem>();
                _GameItems.AddRange(Template.ChildrenTemplateIds.Select(c =>
                {
                    dic["tid"] = c;
                    GameItem gameItem = new GameItem();
                    gameItem.Initialize(service, dic);
                    return gameItem;
                }));
                db.AddRange(_GameItems); //将直接孩子加入数据库
            }
            db.Add(new GameActionRecord
            {
                ParentId = Id,
                ActionId = "Created",
                PropertiesString = $"CreateBy=CreateChar",
            });
        }

        protected override void LoadedCore(IServiceProvider service, IReadOnlyDictionary<string, object> parameters)
        {
            base.LoadedCore(service, parameters);
            Debug.Assert(GameUser != null && GameUser.DbContext != null);
            //未发送给客户端的数据
            var exProp = ExtendProperties.FirstOrDefault(c => c.Name == ChangesItemExPropertyName);
            if (null != exProp)    //若有需要反序列化的对象
            {
                var tmp = JsonSerializer.Deserialize<List<ChangesItemSummary>>(exProp.Text);
                _ChangesItems = ChangesItemSummary.ToChangesItem(tmp, this);
            }

            //加载扩展属性
            SpecificExpandProperties = DbContext.Set<CharSpecificExpandProperty>().Find(Id);
            ////补足角色的槽
            //var tt = World.ItemTemplateManager.GetTemplateFromeId(e.GameChar.TemplateId);
            //List<Guid> ids = new List<Guid>();
            //tt.ChildrenTemplateIds.ApartWithWithRepeated(e.GameChar.GameItems, c => c, c => c.TemplateId, ids, null, null);
            //foreach (var item in ids.Select(c => World.ItemTemplateManager.GetTemplateFromeId(c)))
            //{
            //    var gameItem = World.ItemManager.CreateGameItem(item, e.GameChar.Id);
            //    e.GameChar.GameUser.DbContext.Set<GameItem>().Add(gameItem);
            //    e.GameChar.GameItems.Add(gameItem);
            //}
            ////补足所属物品的槽
            //World.ItemManager.Normalize(e.GameChar.GameItems);
            //通知直接所属物品加载完毕
            var list = GameItems.ToList();
            list.ForEach(c => c.Loaded(service, DbContext));
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
                    _GameItems = DbContext.Set<GameItem>().Where(c => c.OwnerId == Id).Include(c => c.Children).ThenInclude(c => c.Children).ThenInclude(c => c.Children).ToList();
                    _GameItems.ForEach(c => c.GameChar = this);
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
        /// 所属用户Id。
        /// </summary>
        [ForeignKey(nameof(GameUser))]
        public Guid GameUserId { get; set; }

        /// <summary>
        /// 所属用户的导航属性。
        /// </summary>
        public virtual GameUser GameUser { get; set; }

        /// <summary>
        /// 角色显示用的名字。就是昵称，不可重复。
        /// </summary>
        [MaxLength(64)]
        public string DisplayName { get; set; }

        /// <summary>
        /// 用户所处地图区域的Id,这也可能是战斗关卡的Id。如果没有在战斗场景中，则可能是空。
        /// </summary>
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
            base.PrepareSaving(db);
        }

        private List<ChangeItem> _ChangesItems = new List<ChangeItem>();

        /// <summary>
        /// 保存未能发送给客户端的变化数据。
        /// </summary>
        public List<ChangeItem> ChangesItems => _ChangesItems;

        /// <summary>
        /// 未发送给客户端的数据保存在<see cref="GameThingBase.ExtendProperties"/>中使用的属性名称。
        /// </summary>
        public const string ChangesItemExPropertyName = "{BAD410C8-6393-44B4-9EB1-97F91ED11C12}";

        /// <summary>
        /// 角色对象的扩展属性的导航属性。
        /// </summary>
        //[ForeignKey(nameof(Id))]
        public virtual CharSpecificExpandProperty SpecificExpandProperties { get; set; }

        /// <summary>
        /// 客户端的属性。
        /// </summary>
        [NotMapped]
        public Dictionary<string, string> ClientProperties
        {
            get
            {
                return ExtendPropertyDictionary.GetOrAdd(GameExtendProperty.ClientPropertyName, c => new ExtendPropertyDescriptor()
                {
                    Name = GameExtendProperty.ClientPropertyName,
                    Data = new Dictionary<string, string>(),
                    Type = typeof(Dictionary<string, string>),
                    IsPersistence = true,
                }).Data as Dictionary<string, string>;
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
                    // TODO: 释放托管状态(托管对象)
                    _GameItems?.ForEach(c => (c as IDisposable)?.Dispose());  //对独占拥有的子对象调用处置
                }
                // TODO: 释放未托管的资源(未托管的对象)并重写终结器
                // TODO: 将大型字段设置为 null
                _GameItems = null;
                _ChangesItems = null;
                GameUser = null;
                SpecificExpandProperties = null;
                base.Dispose(disposing);
            }
        }

        /// <summary>
        /// 获取此对象所处的用户数据库上下文对象。
        /// </summary>
        [NotMapped]
        public override DbContext DbContext => GameUser.DbContext;

        // // TODO: 仅当“Dispose(bool disposing)”拥有用于释放未托管资源的代码时才替代终结器
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
        /// <param _Name="obj"></param>
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
        /// <param _Name="obj"></param>
        /// <param _Name="gameChar"></param>
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

        private Dictionary<string, FastChangingProperty> _Name2FastChangingProperty;

        protected GameCharBase()
        {
        }

        protected GameCharBase(Guid id) : base(id)
        {
        }

        /// <summary>
        /// 快速变化属性。
        /// </summary>
        [NotMapped]
        public Dictionary<string, FastChangingProperty> Name2FastChangingProperty
        {
            get
            {
                if (_Name2FastChangingProperty is null)
                {
                    lock (this)
                        if (_Name2FastChangingProperty is null)
                        {
                            var list = FastChangingPropertyExtensions.FromGameThing(this);
                            var charId = Id;
                            foreach (var item in list)
                            {
                                item.Tag = (charId, Id);    //设置Tag
                            }
                            _Name2FastChangingProperty = list.ToDictionary(c => c.Name);
                        }
                }
                return _Name2FastChangingProperty;
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (!IsDisposed)
            {
                if (disposing)
                {
                    // TODO: 释放托管状态(托管对象)
                }

                // TODO: 释放未托管的资源(未托管的对象)并重写终结器
                // TODO: 将大型字段设置为 null
                _Name2FastChangingProperty = null;
                base.Dispose(disposing);
            }
        }

        // // TODO: 仅当“Dispose(bool disposing)”拥有用于释放未托管资源的代码时才替代终结器
        // ~GameCharBase()
        // {
        //     // 不要更改此代码。请将清理代码放入“Dispose(bool disposing)”方法中
        //     Dispose(disposing: false);
        // }
    }
}
