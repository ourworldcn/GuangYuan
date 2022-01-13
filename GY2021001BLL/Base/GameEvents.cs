using GuangYuan.GY001.BLL;
using GuangYuan.GY001.TemplateDb;
using GuangYuan.GY001.UserDb;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.ObjectPool;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;

namespace OW.Game
{
    public class GameEventsManagerOptions
    {

    }

    public class SimplePropertyChangedItem<T>
    {
        public SimplePropertyChangedItem()
        {

        }

        public SimplePropertyChangedItem(string name)
        {
            Name = name;
        }

        /// <summary>
        /// 构造函数。
        /// 无论<paramref name="newValue"/>和<paramref name="oldValue"/>给定任何值，<see cref="HasOldValue"/>和<see cref="HasNewValue"/>都设置为true。
        /// </summary>
        /// <param name="name"></param>
        /// <param name="oldValue"></param>
        /// <param name="newValue"></param>
        public SimplePropertyChangedItem(string name, T oldValue, T newValue)
        {
            Name = name;
            OldValue = oldValue;
            NewValue = newValue;
            HasOldValue = HasNewValue = true;
        }

        /// <summary>
        /// 属性的名字。
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 指示<see cref="OldValue"/>中的值是否有意义。
        /// </summary>
        public bool HasOldValue { get; set; }

        /// <summary>
        /// 就值。
        /// </summary>
        public T OldValue { get; set; }

        /// <summary>
        /// 指示<see cref="NewValue"/>中的值是否有意义。
        /// </summary>
        public bool HasNewValue { get; set; }

        /// <summary>
        /// 新值。
        /// </summary>
        public T NewValue { get; set; }

        public DateTime DateTimeUtc { get; set; } = DateTime.UtcNow;
    }

    public class SimplePropertyChangedCollection : ICollection<SimplePropertyChangedItem<object>>
    {
        public SimplePropertyChangedCollection()
        {
        }

        public GameThingBase Thing { get; set; }

        public List<SimplePropertyChangedItem<object>> Items { get; } = new List<SimplePropertyChangedItem<object>>();

        public object Tag { get; set; }

        #region ICollection<SimplePropertyChangedItem<object>>接口

        public int Count => ((ICollection<SimplePropertyChangedItem<object>>)Items).Count;

        public bool IsReadOnly => ((ICollection<SimplePropertyChangedItem<object>>)Items).IsReadOnly;

        public void Add(SimplePropertyChangedItem<object> item)
        {
            ((ICollection<SimplePropertyChangedItem<object>>)Items).Add(item);
        }

        public void Clear()
        {
            ((ICollection<SimplePropertyChangedItem<object>>)Items).Clear();
        }

        public bool Contains(SimplePropertyChangedItem<object> item)
        {
            return ((ICollection<SimplePropertyChangedItem<object>>)Items).Contains(item);
        }

        public void CopyTo(SimplePropertyChangedItem<object>[] array, int arrayIndex)
        {
            ((ICollection<SimplePropertyChangedItem<object>>)Items).CopyTo(array, arrayIndex);
        }

        public IEnumerator<SimplePropertyChangedItem<object>> GetEnumerator()
        {
            return ((IEnumerable<SimplePropertyChangedItem<object>>)Items).GetEnumerator();
        }

        public bool Remove(SimplePropertyChangedItem<object> item)
        {
            return ((ICollection<SimplePropertyChangedItem<object>>)Items).Remove(item);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)Items).GetEnumerator();
        }
        #endregion ICollection<SimplePropertyChangedItem<object>>接口

    }

    /// <summary>
    /// 属性变化的集合。
    /// </summary>
    public class DynamicPropertyChangedCollection : ICollection<SimplePropertyChangedCollection>
    {
        public DynamicPropertyChangedCollection()
        {

        }

        public List<SimplePropertyChangedCollection> Items { get; } = new List<SimplePropertyChangedCollection>();

        #region ICollection<DynamicPropertyChangedItem>接口

        public int Count => ((ICollection<SimplePropertyChangedCollection>)Items).Count;

        public bool IsReadOnly => ((ICollection<SimplePropertyChangedCollection>)Items).IsReadOnly;

        public void Add(SimplePropertyChangedCollection item)
        {
            var changedItem = GetOrAddItem(item.Thing);
            changedItem.Items.AddRange(item.Items);
        }

        public void Clear()
        {
            ((ICollection<SimplePropertyChangedCollection>)Items).Clear();
        }

        public bool Contains(SimplePropertyChangedCollection item)
        {
            return ((ICollection<SimplePropertyChangedCollection>)Items).Contains(item);
        }

        public void CopyTo(SimplePropertyChangedCollection[] array, int arrayIndex)
        {
            ((ICollection<SimplePropertyChangedCollection>)Items).CopyTo(array, arrayIndex);
        }

        public IEnumerator<SimplePropertyChangedCollection> GetEnumerator()
        {
            return ((IEnumerable<SimplePropertyChangedCollection>)Items).GetEnumerator();
        }

        public bool Remove(SimplePropertyChangedCollection item)
        {
            return ((ICollection<SimplePropertyChangedCollection>)Items).Remove(item);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)Items).GetEnumerator();
        }
        #endregion ICollection<DynamicPropertyChangedItem>接口

        /// <summary>
        /// 获取或添加变化项。
        /// </summary>
        /// <param name="thing"></param>
        /// <returns></returns>
        public SimplePropertyChangedCollection GetOrAddItem(GameThingBase thing)
        {
            var result = Items.FirstOrDefault(c => c.Thing.Id == thing.Id);
            if (result is null)
            {
                result = new SimplePropertyChangedCollection() { Thing = thing };
                Items.Add(result);
            }
            return result;
        }

        /// <summary>
        /// 记录指定的动态属性的旧值。
        /// </summary>
        /// <param name="thing"></param>
        /// <param name="keyName"></param>
        public void Mark(GameThingBase thing, string keyName)
        {
            var item = GetOrAddItem(thing);
            var hasOld = thing.Properties.TryGetValue(keyName, out var oldVal);
            var result = new SimplePropertyChangedItem<object>(keyName, oldVal, default) { HasOldValue = hasOld };
            item.Add(result);
        }

        /// <summary>
        /// 记录属性当前值，并设置为新值，记录这一更改。
        /// </summary>
        /// <param name="thing"></param>
        /// <param name="keyName"></param>
        /// <param name="newValue">无论设置任何值，总会将<see cref="SimplePropertyChangedItem{T}.HasNewValue"/>设置为true。</param>
        public void MarkAndSet(GameThingBase thing, string keyName, object newValue)
        {
            var item = GetOrAddItem(thing);
            var hasOld = thing.Properties.TryGetValue(keyName, out var oldVal);
            var result = new SimplePropertyChangedItem<object>(keyName, oldVal, newValue) { HasOldValue = hasOld, HasNewValue = true, };
            item.Add(result);
            thing.Properties[keyName] = newValue;
        }

        /// <summary>
        /// 记录原始值后删除属性。
        /// </summary>
        /// <param name="thing"></param>
        /// <param name="keyName"></param>
        /// <returns></returns>
        public bool MarkAndRemove(GameThingBase thing, string keyName)
        {
            var item = GetOrAddItem(thing);
            var hasOld = thing.Properties.Remove(keyName, out _);
            var result = new SimplePropertyChangedItem<object>(keyName) { OldValue = hasOld, HasOldValue = hasOld, HasNewValue = false, };
            item.Add(result);
            return hasOld;
        }
    }

    /// <summary>
    /// 事件服务。
    /// </summary>
    public class GameEventsManager : GameManagerBase<GameEventsManagerOptions>
    {
        #region 构造函数

        /// <summary>
        /// 构造函数。
        /// </summary>
        public GameEventsManager()
        {
            Initialize();
        }

        public GameEventsManager(IServiceProvider service) : base(service)
        {
            Initialize();
        }

        public GameEventsManager(IServiceProvider service, GameEventsManagerOptions options) : base(service, options)
        {
            Initialize();
        }

        private void Initialize()
        {
        }
        #endregion 构造函数

        #region 动态属性变化

        public virtual void OnDynamicPropertyChanged(DynamicPropertyChangedCollection args)
        {
            var lvName = World.PropertyManager.LevelPropertyName;
            foreach (var item in args)
            {
                foreach (var subItem in item)
                {
                    if (subItem.Name == lvName)
                    {
                        OnLvChanged(item.Thing, (int)Convert.ToDecimal(subItem.NewValue));
                        break;
                    }
                }
            }
        }

        public virtual bool OnLvChanged(GameThingBase gameThing, int oldLv)
        {
            gameThing.RemoveFastChangingProperty(ProjectConstant.UpgradeTimeName);
            //扫描成就
            if (!(gameThing is GameChar gc))
                gc = (gameThing as GameItem)?.GameChar;
            if (null != gc)
                World.MissionManager.ScanAsync(gc);
            return false;
        }

        /// <summary>
        /// 物品/道具添加到容器后被调用。
        /// </summary>
        /// <param name="gameItems">添加的对象，元素中的关系属性已经被正常设置。</param>
        /// <param name="parameters">参数。</param>
        public virtual void OnGameItemAdd(IEnumerable<GameItem> gameItems, Dictionary<string, object> parameters)
        {

        }

        #endregion 动态属性变化

        #region 创建后初始化


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void GameItemCreated(GameItem gameItem, Guid templateId, [AllowNull] GameItem parent, Guid? ownerId, [AllowNull] IReadOnlyDictionary<string, object> parameters = null)
        {
            GameItemCreated(gameItem, World.ItemTemplateManager.GetTemplateFromeId(templateId), parent, ownerId, parameters);
        }

        /// <summary>
        /// 初始化一个<see cref="GameItem"/>对象。
        /// </summary>
        /// <param name="gameItem"></param>
        /// <param name="template"></param>
        /// <param name="parent">父容器对象,如果为null，则使用<paramref name="ownerId"/>设置拥有者属性，否则忽略<paramref name="ownerId"/></param>
        /// <param name="ownerId"></param>
        /// <param name="parameters"></param>
        public virtual void GameItemCreated(GameItem gameItem, [NotNull] GameItemTemplate template, [AllowNull] GameItem parent, Guid? ownerId, [AllowNull] IReadOnlyDictionary<string, object> parameters = null)
        {
            //设置本类型特有属性
            GameThingCreated(gameItem, template, parameters);
            var gpm = World.PropertyManager;
            var gt = gameItem.Template;
            if (gt.Properties.TryGetValue("Count", out var countObj) || gt.Properties.TryGetValue("count", out countObj)) //若指定了初始数量
                gameItem.Count = Convert.ToDecimal(countObj);
            else
                gameItem.Count ??= gt.Properties.ContainsKey(gpm.StackUpperLimit) ? 0 : 1;
            //设置导航关系
            if (parent is null)
                gameItem.OwnerId = ownerId;
            else
            {
                gameItem.ParentId = parent.Id;
                gameItem.Parent = parent;
            }
            //追加子对象
            if (gt.ChildrenTemplateIds.Count > 0)
            {
                gameItem.Children.AddRange(gt.ChildrenTemplateIds.Select(c =>
                {
                    var r = new GameItem();
                    GameItemCreated(r, c, gameItem, null, parameters);
                    return r;
                }));
            }
            //追加一些可识别的属性
            if (null != parameters)
            {
                if (parameters.TryGetValue("ptid", out var tmp))
                    gameItem.Properties["ptid"] = tmp;
                if (parameters.TryGetValue("count", out tmp) && OwConvert.TryToDecimal(tmp, out var deci) && gameItem.GameChar != null) //若可以设置
                    gameItem.Count = deci;
            }

        }

        private static readonly string[] _GameItemCreatedKeyNames = new string[] { "tid", "tt", "count", "Count", "ownerid", "parent", "ptid" };

        /// <summary>
        /// 初始化一个物品，
        /// </summary>
        /// <param name="gameItem"></param>
        /// <param name="propertyBag">tid(模板id),tt(直接指定模板对象，该属性优先于tid属性，必须是GameThingTemplateBase对象或其派生类),未指定模板不会报错。
        /// count或Count(数量),如果存在则强行指定数量。否则若指定了模板则在模板内寻找数量属性，再次，在GameThingCreated后Count属性还是空，则堆叠物品初始化为0，非堆叠物品初始化为1.
        /// ptid(父容器模板id),parent(直接指定父容器对象，必须是GameItem或其派生类),ownerid(拥有者id属性)属性。ownerid最优先，它存在则忽略另外两个属性，parent次之，忽略ptid属性，两者都没有则会试图找到ptid属性并记录以备之后使用
        /// </param>
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public virtual void GameItemCreated([NotNull] GameItem gameItem, [NotNull] IReadOnlyDictionary<string, object> propertyBag)
        {
            //设置本类型特有属性
            //设置模板
            if (propertyBag.TryGetValue("tt", out var ttObj) && ttObj is GameThingTemplateBase tt)
                GameThingCreated(gameItem, tt, propertyBag);
            else if (propertyBag.TryGetGuid("tid", out var tid))
                GameThingCreated(gameItem, tid, propertyBag);
            //设置数量
            var gpm = World.PropertyManager;
            tt = gameItem.Template;
            if (propertyBag.TryGetDecimal("Count", out var count) || propertyBag.TryGetDecimal("count", out count)) //若指定了初始数量
                gameItem.Count = count;
            else if (null != tt)
            {
                if (tt.Properties.TryGetDecimal("Count", out count) || tt.Properties.TryGetDecimal("count", out count)) //若制定了模板且其中指定了初始数量
                    gameItem.Count = count;
                else
                    gameItem.Count ??= tt.Properties.ContainsKey(gpm.StackUpperLimit) ? 0 : 1;
            }
            //设置导航关系
            if (propertyBag.TryGetGuid("ownerid", out var ownerid)) //若指定了拥有者id
                gameItem.OwnerId = ownerid;
            else if (propertyBag.TryGetValue("parent", out var parentObj) && parentObj is GameItem parent) //若指定了父容器
            {
                gameItem.ParentId = parent.Id;
                gameItem.Parent = parent;
            }
            else if (propertyBag.TryGetValue("ptid", out var ptid))
                gameItem.Properties["ptid"] = ptid;
            //追加子对象
            if (tt.ChildrenTemplateIds.Count > 0)   //若存在子对象
            {
                var subpb = DictionaryPool<string, object>.Shared.Get();    //漏掉返回池中不是大问题
                OwHelper.Copy(propertyBag, subpb, c => !_GameItemCreatedKeyNames.Contains(c));
                subpb["parent"] = gameItem; //指向自己作为父容器
                foreach (var item in tt.ChildrenTemplateIds)
                {
                    var gi = new GameItem();
                    gameItem.Children.Add(gi);
                    subpb["tid"] = item.ToString();  //设置模板Id
                    GameItemCreated(gi, subpb);
                }
                DictionaryPool<string, object>.Shared.Return(subpb);
            }
        }

        /// <summary>
        /// 初始化角色对象。
        /// </summary>
        /// <param name="gameChar"></param>
        /// <param name="template"></param>
        /// <param name="user">null则不设置自身的导航属性。</param>
        /// <param name="displayName">昵称。为null则不设置</param>
        /// <param name="parameters"></param>
        public virtual void GameCharCreated(GameChar gameChar, GameItemTemplate template, [AllowNull] GameUser user, [AllowNull] string displayName, [AllowNull] IReadOnlyDictionary<string, object> parameters)
        {
            GameThingCreated(gameChar, template, parameters);
            var gt = gameChar.Template;
            //初始化本类型特殊数据
            if (null != user)
            {
                gameChar.GameUserId = user.Id;
                gameChar.GameUser = user;
            }
            if (null != displayName)
                gameChar.DisplayName = displayName;
            //追加子对象
            if (gt.ChildrenTemplateIds.Count > 0)
            {
                Dictionary<string, object> dic = new Dictionary<string, object>()
                {
                    {"owner",this },
                };
                var coll = gt.ChildrenTemplateIds.Select(c =>
                    {
                        GameItem gi = new GameItem() { GameChar = gameChar };
                        GameItemCreated(gi, c, null, gameChar.Id, dic);
                        return gi;
                    }).ToArray();
                gameChar.GameItems.AddRange(coll);
                gameChar.DbContext.AddRange(coll); //将直接孩子加入数据库
            }
        }

        /// <summary>
        /// 初始化一个新用户。
        /// </summary>
        /// <param name="user"></param>
        /// <param name="loginName"></param>
        /// <param name="pwd"></param>
        /// <param name="context">使用的存储上下文，如果是null，则会自动创建一个。</param>
        /// <param name="parameters"></param>
        public virtual void GameUserCreated(GameUser user, string loginName, string pwd, [AllowNull] DbContext context, [AllowNull] IReadOnlyDictionary<string, object> parameters)
        {
            //初始化本类型的数据
            user.Services = Service;
            user.DbContext ??= (context ?? World.CreateNewUserDbContext());

            user.CurrentToken = Guid.NewGuid();
            user.LoginName = loginName;

            using var hash = (HashAlgorithm)Service.GetService(typeof(HashAlgorithm));
            var pwdHash = hash.ComputeHash(Encoding.UTF8.GetBytes(pwd));
            user.PwdHash = pwdHash;

            user.DbContext.Add(user);
        }

        #region 保护方法
        /// <summary>
        /// 
        /// </summary>
        /// <param name="thing"></param>
        /// <param name="templateId"></param>
        /// <param name="parameters"></param>
        protected void GameThingCreated(GameThingBase thing, Guid templateId, IReadOnlyDictionary<string, object> parameters)
        {
            var template = World.ItemTemplateManager.GetTemplateFromeId(templateId);
            GameThingCreated(thing, template, parameters);
        }

        /// <summary>
        /// 在一个<see cref="GameThingBase"/>的子类被创建后调用。
        /// </summary>
        /// <param name="thing"></param>
        /// <param name="template"></param>
        /// <param name="parameters"></param>
        protected virtual void GameThingCreated(GameThingBase thing, GameThingTemplateBase template, IReadOnlyDictionary<string, object> parameters)
        {
            var gpm = World.PropertyManager;
            //初始化自身属性
            thing.TemplateId = template.Id;
            thing.Template = template;
            var coll = gpm is null ? template.Properties : gpm.Filter(template.Properties);
            var dic = thing.Properties;
            foreach (var item in coll)   //复制属性
            {
                if (item.Value is IList seq)   //若是属性序列
                {
                    var indexPn = template.GetIndexPropName(item.Key);
                    var lv = Convert.ToInt32(template.Properties.GetValueOrDefault(indexPn, 0m));
                    dic[item.Key] = seq[Math.Clamp(lv, 0, seq.Count - 1)];
                }
                else
                    dic[item.Key] = item.Value;
            }
            if (template.SequencePropertyNames.Length > 0 && !dic.Keys.Any(c => c.StartsWith(GameThingTemplateBase.LevelPrefix))) //若需追加等级属性
                dic[gpm.LevelPropertyName] = 0m;
#if DEBUG
            dic["tname"] = template.DisplayName.Replace('，', '-').Replace(',', '-').Replace('=', '-');
#endif

        }
        #endregion 保护方法

        #endregion 创建后初始化

        #region 转换为字典属性包

        /// <summary>
        /// 将指定对象的主要属性提取到指定字典中，以备可以使用<see cref="GameItemCreated(GameItem, IReadOnlyDictionary{string, object})"/>进行恢复。
        /// </summary>
        /// <param name="gameItem"></param>
        /// <param name="propertyBag">可以处理"tid", "count", "ownerid", "ptid" 等几个属性。</param>
        /// <param name="prefix"></param>
        /// <param name="suffix"></param>
        public virtual void Copy(GameItem gameItem, IDictionary<string, object> propertyBag, string prefix = null, string suffix = null)
        {
            //{ "tid", "tt", "count", "Count", "ownerid", "parent", "ptid" };
            prefix ??= string.Empty;
            suffix ??= string.Empty;
            propertyBag[$"{prefix}tid{suffix}"] = gameItem.TemplateId.ToString();
            if (gameItem.Count != null)
                propertyBag[$"{prefix}count{suffix}"] = gameItem.Count;
            if (gameItem.OwnerId != null)
                propertyBag[$"{prefix}ownerid{suffix}"] = gameItem.OwnerId;
            else if (gameItem.Parent != null)
                propertyBag[$"{prefix}ptid{suffix}"] = gameItem.Parent.TemplateId.ToString();
            else if (gameItem.Properties.TryGetGuid("ptid", out var ptid))
                propertyBag[$"{prefix}ptid{suffix}"] = ptid.ToString();
        }

        #endregion 转换为字典属性包

        #region 加载后初始化
        public virtual void GameItemLoaded(GameItem gameItem)
        {
            GameThingLoaded(gameItem);
            //通知直接所属物品加载完毕
            var list = gameItem.Children.ToArray();
            Array.ForEach(list, c => GameItemLoaded(c));
        }

        public virtual void GameCharLoaded(GameChar gameChar)
        {
            GameThingLoaded(gameChar);
            Debug.Assert(gameChar.GameUser != null && gameChar.GameUser.DbContext != null);
            //补足角色的槽
            var tt = gameChar.Template;
            var ids = tt.ChildrenTemplateIds.ToList();
            var list = gameChar.GameItems.ToList();
            var exists = list.Select(c => c.TemplateId).ToList();
            for (int i = ids.Count - 1; i >= 0; i--)
            {
                var tid = ids[i];
                if (!exists.Remove(tid))    //若缺少槽
                {
                    var gi = new GameItem();
                    GameItemCreated(gi, tid, null, gameChar.Id);
                    gameChar.GameItems.Add(gi);
                }
            }
            //通知直接所属物品加载完毕
            var tmp = gameChar.GameItems.ToArray();
            Array.ForEach(tmp, c =>
            {
                c.GameChar = gameChar;
                GameItemLoaded(c);
            });
            //增加推关战力
            World.CombatManager.UpdatePveInfo(gameChar);
        }

        public virtual void GameUserLoaded(GameUser user, DbContext context)
        {
            //初始化本类型的数据
            user.Services = Service;
            user.DbContext = context;
            user.CurrentToken = Guid.NewGuid();
        }

        /// <summary>
        /// 加载后初始化。
        /// </summary>
        /// <param name="thing"></param>
        protected virtual void GameThingLoaded(GameThingBase thing)
        {
            thing.Template = World.ItemTemplateManager.GetTemplateFromeId(thing.TemplateId);
        }
        #endregion 加载后初始化

        #region Json反序列化
        public virtual void ThingBaseJsonDeserialized(GameThingBase thingBase)
        {
            thingBase.Template = World.ItemTemplateManager.GetTemplateFromeId(thingBase.TemplateId);
            var db = thingBase.DbContext;
            db.AddRange(thingBase.ExtendProperties);
        }

        public virtual void JsonDeserialized(GameItem gameItem)
        {
            ThingBaseJsonDeserialized(gameItem);
            gameItem.Children.ForEach(c => c.Parent = gameItem);
            gameItem.Children.ForEach(c => JsonDeserialized(c));
        }

        public virtual void JsonDeserialized(GameChar gameChar)
        {
            ThingBaseJsonDeserialized(gameChar);
            gameChar.DbContext.AddRange(gameChar.GameItems);
            gameChar.GameItems.ForEach(c => c.GameChar = gameChar);
            gameChar.GameItems.ForEach(c => JsonDeserialized(c));
        }

        public virtual void JsonDeserialized(GameUser gameUser)
        {
            gameUser.Services ??= Service;
            gameUser.DbContext ??= gameUser.Services.GetService<VWorld>().CreateNewUserDbContext();
            gameUser.GameChars.ForEach(c => c.GameUser = gameUser);
            gameUser.GameChars.ForEach(c => JsonDeserialized(c));
            gameUser.DbContext.Add(gameUser);
        }
        #endregion Json反序列化

        #region 复制信息相关

        /// <summary>
        /// 复制账号信息。
        /// </summary>
        /// <param name="src"></param>
        /// <param name="dest"></param>
        public virtual void Clone(GameUser src, GameUser dest)
        {
            dest.Services ??= Service;
            var world = dest.Services.GetRequiredService<VWorld>();
            dest.DbContext ??= world.CreateNewUserDbContext();
            dest.Region = src.Region;
            OwHelper.Copy(src.Properties, dest.Properties);
            Clone(src.ExtendProperties, dest.ExtendProperties, dest.Id);
            foreach (var item in src.GameChars)
            {
                var gc = new GameChar()
                {
                    GameUserId = dest.Id,
                    GameUser = dest,
                };
                dest.GameChars.Add(gc);
                Clone(item, gc);
            }
            dest.DbContext.Add(dest);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="src"></param>
        /// <param name="dest"></param>
        public virtual void Clone(GameChar src, GameChar dest)
        {
            CloneThingBase(src, dest);
            dest.CharType = src.CharType;
            dest.CombatStartUtc = src.CombatStartUtc;
            dest.CurrentDungeonId = src.CurrentDungeonId;
            dest.DisplayName = CnNames.GetName(World.IsHit(0.5));
            List<GameItem> list = new List<GameItem>();
            foreach (var item in src.GameItems)
            {
                var gi = new GameItem()
                {
                    OwnerId = dest.Id,
                    GameChar = dest,
                };
                dest.GameItems.Add(gi);
                Clone(item, gi);
                list.Add(gi);
            }
            dest.DbContext.AddRange(list);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="src"></param>
        /// <param name="dest"></param>
        public virtual void Clone(GameItem src, GameItem dest)
        {
            CloneThingBase(src, dest);
            dest.Count = src.Count;
            foreach (var item in src.Children)
            {
                var subItem = new GameItem()
                {
                    ParentId = dest.Id,
                    Parent = dest,
                };
                dest.Children.Add(subItem);
                Clone(item, subItem);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="src"></param>
        /// <param name="dest"></param>
        public virtual void CloneThingBase(GameThingBase src, GameThingBase dest)
        {
            dest.TemplateId = src.TemplateId;
            dest.Template = src.Template;
            dest.ClientGutsString = src.ClientGutsString;
            dest.ExPropertyString = src.ExPropertyString;
            OwHelper.Copy(src.Properties, dest.Properties);
            if (null != dest.DbContext)
                Clone(src.ExtendProperties, dest.ExtendProperties, dest.Id);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="src"></param>
        /// <param name="dest"></param>
        /// <param name="destId">目标<see cref="GameExtendProperty"/>对象的Id属性。</param>
        public void Clone(IEnumerable<GameExtendProperty> src, ICollection<GameExtendProperty> dest, Guid destId)
        {
            foreach (var item in src)
            {
                var exp = new GameExtendProperty(item.Name, destId) { };
                CloneExtendProperty(item, exp, destId);
                dest.Add(exp);
            }
        }

        /// <summary>
        /// 克隆一个新的<see cref="GameExtendProperty"/>对象。
        /// </summary>
        /// <param name="src"></param>
        /// <param name="dest"></param>
        /// <param name="destId">目标<see cref="GameExtendProperty"/>对象的 Id 属性。</param>
        public void CloneExtendProperty(GameExtendProperty src, GameExtendProperty dest, Guid destId)
        {
            dest.Id = destId;
            dest.Name = src.Name;
            dest.ByteArray = (byte[])src.ByteArray?.Clone();
            dest.DateTimeValue = src.DateTimeValue;
            dest.DecimalValue = src.DecimalValue;
            dest.GuidValue = src.GuidValue;
            dest.IntValue = src.IntValue;
            dest.StringValue = src.StringValue;
            dest.Text = src.Text;
            OwHelper.Copy(src.Properties, dest.Properties);
        }
        #endregion 复制信息相关
    }

    public static class GameEventsManagerExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void GameItemCreated(this GameEventsManager manager, GameItem gameItem, Guid templateId) =>
                   manager.GameItemCreated(gameItem, manager.World.ItemTemplateManager.GetTemplateFromeId(templateId));


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void GameItemCreated(this GameEventsManager manager, GameItem gameItem, GameItemTemplate template) =>
                    manager.GameItemCreated(gameItem, template, null, null);
    }


}
