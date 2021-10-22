using GuangYuan.GY001.BLL;
using GuangYuan.GY001.TemplateDb;
using GuangYuan.GY001.UserDb;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections;
using System.Collections.Generic;
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
            var hasOld = thing.Properties.Remove(keyName, out var oldVal);
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
        public virtual void GameItemCreated(GameItem gameItem, GameItemTemplate template, [AllowNull] GameItem parent, Guid? ownerId, [AllowNull] IReadOnlyDictionary<string, object> parameters = null)
        {
            var gpm = World.PropertyManager;
            //设置本类型特有属性
            GameThingCreated(gameItem, template, parameters);
            var gt = gameItem.Template;
            if (gt.Properties.TryGetValue("Count", out var countObj)) //若指定了初始数量
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
        }

        /// <summary>
        /// 初始化角色对象。
        /// </summary>
        /// <param name="gameChar"></param>
        /// <param name="template"></param>
        /// <param name="user">null则不设置自身的导航属性。</param>
        /// <param name="displayName">昵称。为null则不设置</param>
        /// <param name="parameters"></param>
        /// 
        public virtual void GameCharCreated(GameChar gameChar, GameItemTemplate template, [AllowNull] GameUser user, [AllowNull] string displayName, IReadOnlyDictionary<string, object> parameters)
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
                    });
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

        #region 加载后初始化
        public virtual void GameItemLoaded(GameItem gameItem)
        {
            GameThingLoaded(gameItem);
            //通知直接所属物品加载完毕
            var list = gameItem.Children.ToList();
            list.ForEach(c => GameItemLoaded(c));

        }

        public virtual void GameCharLoaded(GameChar gameChar)
        {
            GameThingLoaded(gameChar);
            Debug.Assert(gameChar.GameUser != null && gameChar.GameUser.DbContext != null);
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
            var list = gameChar.GameItems.ToList();
            list.ForEach(c =>
            {
                c.GameChar = gameChar;
                GameItemLoaded(c);
            });
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
