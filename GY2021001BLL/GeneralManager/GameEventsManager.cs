using GuangYuan.GY001.BLL;
using GuangYuan.GY001.BLL.GeneralManager;
using GuangYuan.GY001.TemplateDb;
using GuangYuan.GY001.UserDb;
using GuangYuan.GY001.UserDb.Social;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.ObjectPool;
using OW.Game.PropertyChange;
using OW.Game.Store;
using System;
using System.Buffers;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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

    public class SimplePropertyChangedCollection : Collection<GamePropertyChangeItem<object>>
    {
        public SimplePropertyChangedCollection()
        {
        }

        public GameThingBase Thing { get; set; }

        public object Tag { get; set; }


    }

    /// <summary>
    /// 属性变化的集合。
    /// </summary>
    public class DynamicPropertyChangedCollection : Collection<SimplePropertyChangedCollection>
    {
        public DynamicPropertyChangedCollection()
        {
        }

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
            var result = new GamePropertyChangeItem<object>(null, name: keyName, oldValue: oldVal, newValue: default) { HasOldValue = hasOld };
            item.Add(result);
        }

        /// <summary>
        /// 记录属性当前值，并设置为新值，记录这一更改。
        /// </summary>
        /// <param name="thing"></param>
        /// <param name="keyName"></param>
        /// <param name="newValue">无论设置任何值，总会将<see cref="GamePropertyChangedItem{T}.HasNewValue"/>设置为true。</param>
        public void MarkAndSet(GameThingBase thing, string keyName, object newValue)
        {
            var item = GetOrAddItem(thing);
            var hasOld = thing.Properties.TryGetValue(keyName, out var oldVal);
            var result = new GamePropertyChangeItem<object>(null, name: keyName, oldValue: oldVal, newValue: newValue) { HasOldValue = hasOld, HasNewValue = true, };
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
            var result = new GamePropertyChangeItem<object>(null, keyName) { OldValue = hasOld, HasOldValue = hasOld, HasNewValue = false, };
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
            _ItemTemplateManager = World.ItemTemplateManager;
        }
        #endregion 构造函数

        GameItemTemplateManager _ItemTemplateManager;

        public GameItemTemplateManager ItemTemplateManager
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return _ItemTemplateManager;
            }
        }

        #region 基础规则相关

        /// <summary>
        /// 当用户登录后调用。
        /// 系统登录不会调用此函数。此函数在<see cref="GameCharLoaded(GameChar)"/>之后被调用。
        /// 多终端来回踢时，将导致多次调用此函数。
        /// </summary>
        /// <param name="gameChar"></param>
        public virtual void GameCharLogined(GameChar gameChar)
        {
            World.AllianceManager.JoinGuildChatChannel(gameChar);
        }

        #endregion 基础规则相关

        #region 动态属性变化

        /// <summary>
        /// 动态属性发生变化。
        /// </summary>
        /// <param name="arg"></param>
        public virtual void OnPropertyChanged(GamePropertyChangeItem<object> arg)
        {

        }

        public virtual void OnDynamicPropertyChanged(DynamicPropertyChangedCollection args)
        {
            var lvName = World.PropertyManager.LevelPropertyName;
            foreach (var item in args)
            {
                foreach (var subItem in item)
                {
                    if (subItem.PropertyName == lvName)
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
                gc = (gameThing as GameItem)?.GetGameChar();
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

        /// <summary>
        /// 
        /// </summary>
        private static readonly string[] _GameItemCreatedKeyNames = new string[] { "tid", "tt", "count", "Count", "ownerid", "parent", "ptid",
            nameof(GameThingBase.ExtraDecimal),nameof(GameThingBase.ExtraString) };

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
            #region 设置模板
            GameThingCreated(gameItem, propertyBag);
            #endregion 设置模板
            //设置本类型特有属性

            #region 设置数量
            var gpm = World.PropertyManager;
            var tt = gameItem.GetTemplate();
            if (propertyBag.TryGetDecimal("Count", out var count) || propertyBag.TryGetDecimal("count", out count)) //若指定了初始数量
                gameItem.Count = count;
            else if (null != tt)
            {
                if (tt.Properties.TryGetDecimal("Count", out count) || tt.Properties.TryGetDecimal("count", out count)) //若制定了模板且其中指定了初始数量
                    gameItem.Count = count;
                else
                    gameItem.Count ??= tt.Properties.ContainsKey(gpm.StackUpperLimitPropertyName) ? 0 : 1;
            }
            #endregion 设置数量

            #region 设置导航关系
            if (propertyBag.TryGetGuid("ownerid", out var ownerid)) //若指定了拥有者id
            {
                var gc = World.CharManager.GetCharFromId(ownerid);
                gc?.GameItems.Add(gameItem);
                gameItem.OwnerId = ownerid;
            }
            else if (propertyBag.TryGetValue("parent", out var parentObj) && parentObj is GameItem parent) //若指定了父容器
            {
                gameItem.ParentId = parent.Id;
                gameItem.Parent = parent;
            }
            else if (propertyBag.TryGetValue("ptid", out var ptid))
                gameItem.Properties["ptid"] = ptid;
            #endregion 设置导航关系

            #region 追加子对象
            if (tt?.ChildrenTemplateIds.Count > 0)   //若存在子对象
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
            #endregion 追加子对象
        }

        /// <summary>
        /// 寻找指定模板Id的直接孩子对象，若没找到则创建一个。调用者要自行锁定对象以保证临界资源不会受损。
        /// 特别地，对角色和行会对象而言，添加的对象会自动加入数据库，但没有调用保存。
        /// </summary>
        /// <param name="parent">父对象。</param>
        /// <param name="childTId">直属孩子的模板Id。</param>
        /// <returns>孩子对象。</returns>
        public GameItem GetOrCreateChild(GameThingBase parent, Guid childTId)
        {
            var children = World.PropertyManager.GetChildrenCollection(parent);
            var child = children.FirstOrDefault(c => c.ExtraGuid == childTId);
            if (child != null)
                return child;
            //若没有指定TId的孩子
            child = new GameItem();
            var pg = DictionaryPool<string, object>.Shared.Get();
            if (parent is GameItem)
                pg["parent"] = parent;
            else
                pg["ownerid"] = parent.Id;
            pg["tid"] = childTId;
            children.Add(child);
            GameItemCreated(child, pg);
            if (!(parent is GameItem))
                parent.GetDbContext().Add(child);
            DictionaryPool<string, object>.Shared.Return(pg);
            return child;
        }

        /// <summary>
        /// 工会对象创建后调用。
        /// </summary>
        /// <param name="guild"></param>
        /// <param name="propertyBag">DbContext可以指定使用的数据库上下文，如果没有指定则自动创建一个。
        /// CreatorId=创建角色的Id。</param>
        public virtual void GameGuildCreated(GameGuild guild, [NotNull] IReadOnlyDictionary<string, object> propertyBag)
        {
            GameThingCreated(guild, propertyBag);
            //数据库上下文
            var db = propertyBag.GetValueOrDefault("DbContext") as DbContext ?? World.CreateNewUserDbContext();
            guild.RuntimeProperties["DbContext"] = db;
            guild.DisplayName = propertyBag.GetValueOrDefault(nameof(GameGuild.DisplayName)) as string;
            //创建者
            if (propertyBag.TryGetGuid("CreatorId", out var creatorId))
            {
                using var dw = World.CharManager.LockOrLoad(creatorId, out var gu);
                if (dw != null)
                {
                    var gc = gu.GameChars.FirstOrDefault(c => c.Id == creatorId);
                    if (gc != null)
                    {
                        var guildSlot = GetOrCreateChild(gc, ProjectConstant.GuildSlotId);
                        guildSlot.ExtraString = guild.IdString;
                        guildSlot.ExtraDecimal = (int)GuildDivision.会长;
                    }
                }
            }
            //子对象
            foreach (var tid in guild.GetTemplate().ChildrenTemplateIds)   //创建子对象
            {
                var gi = new GameItem();
                guild.Items.Add(gi);
                World.EventsManager.GameItemCreated(gi, World.ItemTemplateManager.GetTemplateFromeId(tid), null, guild.Id);
                db.Add(gi);
            }
        }

        public virtual void GameGuildLoaded(GameGuild guild)
        {
            GameThingLoaded(guild);
            guild.RuntimeProperties["DbContext"] = World.CreateNewUserDbContext();
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
            this.GameThingCreated(gameChar, template, parameters);
            var gt = gameChar.GetTemplate();
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
                        GameItem gi = new GameItem();
                        gi.SetGameChar(gameChar);
                        this.GameItemCreated(gi, c, null, gameChar.Id, dic);
                        return gi;
                    }).ToArray();
                gameChar.GameItems.AddRange(coll);
                gameChar.GetDbContext().AddRange(coll); //将直接孩子加入数据库
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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="thing"></param>
        /// <param name="propertyBag">识别 tt(模板对象，优先),tid模板Id，ExtraString 和 ExtraDecimal属性。若该字典和模板内有可识别的同名属性，则该字典中的属性更有先。
        /// 该字典内不可识别属性一律被忽略。</param>
        public virtual void GameThingCreated(GameThingBase thing, IReadOnlyDictionary<string, object> propertyBag)
        {
            var gpm = World.PropertyManager;
            //初始化自身属性
            GameThingTemplateBase tt = default;
            if (propertyBag.TryGetValue("tt", out var ttObj) && ttObj is GameThingTemplateBase @base)
                tt = @base;
            else if (propertyBag.TryGetGuid("tid", out var tid))
                tt = World.ItemTemplateManager.GetTemplateFromeId(tid);
            if (null != tt)
            {
                thing.ExtraGuid = tt.Id;
                thing.SetTemplate(tt);
                var coll = gpm is null ? tt.Properties as IDictionary<string, object> : new ConcurrentDictionary<string, object>(gpm.Filter(tt.Properties));
                var dic = thing.Properties;
                foreach (var item in coll)   //复制属性
                {
                    if (item.Value is IList seq)   //若是属性序列
                    {
                        var indexPn = tt.GetIndexPropName(item.Key);
                        var lv = Convert.ToInt32(tt.Properties.GetValueOrDefault(indexPn, 0m));
                        dic[item.Key] = seq[Math.Clamp(lv, 0, seq.Count - 1)];
                    }
                    else
                        dic[item.Key] = item.Value;
                }
                if (tt.SequencePropertyNames.Length > 0 && !dic.Keys.Any(c => c.StartsWith(GameThingTemplateBase.LevelPrefix))) //若需追加等级属性
                    dic[gpm.LevelPropertyName] = 0m;
#if DEBUG
                dic["tname"] = tt.DisplayName.Replace('，', '-').Replace(',', '-').Replace('=', '-');
#endif
            }
            //处理特殊属性
            if (DictionaryUtil.TryGetDecimal(nameof(thing.ExtraDecimal), out var dec, propertyBag, tt.Properties))
            {
                thing.Properties.Remove(nameof(thing.ExtraDecimal), out _);
                thing.ExtraDecimal = dec;
            }
            if (DictionaryUtil.TryGetString(nameof(thing.ExtraString), out var str, propertyBag, tt.Properties))
            {
                thing.Properties.Remove(nameof(thing.ExtraString), out _);
                thing.ExtraString = str;
            }
        }

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
            propertyBag[$"{prefix}tid{suffix}"] = gameItem.ExtraGuid.ToString();
            if (gameItem.Count != null)
                propertyBag[$"{prefix}count{suffix}"] = gameItem.Count;
            if (gameItem.OwnerId != null)
                propertyBag[$"{prefix}ownerid{suffix}"] = gameItem.OwnerId;
            else if (gameItem.Parent != null)
                propertyBag[$"{prefix}ptid{suffix}"] = gameItem.Parent.ExtraGuid.ToString();
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
            var tt = gameChar.GetTemplate();
            var ids = tt.ChildrenTemplateIds.ToList();
            var list = gameChar.GameItems.ToList();
            var exists = list.Select(c => c.ExtraGuid).ToList();
            DbContext db = null;
            for (int i = ids.Count - 1; i >= 0; i--)
            {
                var tid = ids[i];
                if (!exists.Remove(tid))    //若缺少槽
                {
                    var gi = new GameItem();
                    this.GameItemCreated(gi, tid, null, gameChar.Id);
                    gameChar.GameItems.Add(gi);
                    db ??= gameChar.GetDbContext();
                    db.Add(gi);
                }
            }
            //通知直接所属物品加载完毕
            var tmp = gameChar.GameItems.ToArray();
            Array.ForEach(tmp, c =>
            {
                c.SetGameChar(gameChar);
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
        protected virtual void GameThingLoaded(GameThingBase thing) =>
            thing.SetTemplate(World.ItemTemplateManager.GetTemplateFromeId(thing.ExtraGuid));
        #endregion 加载后初始化

        #region Json反序列化
        public virtual void ThingBaseJsonDeserialized(GameThingBase thingBase)
        {
            thingBase.SetTemplate(World.ItemTemplateManager.GetTemplateFromeId(thingBase.ExtraGuid));
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
            gameChar.GetDbContext().AddRange(gameChar.GameItems);
            gameChar.GameItems.ForEach(c => c.SetGameChar(gameChar));
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
            dest.DisplayName = World.CharManager.GetNewDisplayName(dest.GameUser);
            List<GameItem> list = new List<GameItem>();
            foreach (var item in src.GameItems)
            {
                var gi = new GameItem()
                {
                    OwnerId = dest.Id,
                };
                gi.SetGameChar(dest);
                dest.GameItems.Add(gi);
                Clone(item, gi);
                list.Add(gi);
            }
            dest.GetDbContext().AddRange(list);
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
            dest.ExtraGuid = src.ExtraGuid;
            dest.SetTemplate(src.GetTemplate());
            dest.ExtraString = src.ExtraString;
            dest.ExtraDecimal = src.ExtraDecimal;
            dest.BinaryArray = src.BinaryArray?.ToArray();
            OwHelper.Copy(src.Properties, dest.Properties);
        }

        #endregion 复制信息相关

        #region 物品相关

        /// <summary>
        /// 根据属性寻找指定的物品对象。
        /// </summary>
        /// <param name="gameChar"></param>
        /// <param name="pb"></param>
        /// <param name="prefix"></param>
        /// <param name="data">将找到的对象和对应的属性字典追加到此集合。如果没有找到对应的物品，则使用null填充Item1,Item2仍然是字典。</param>
        /// <returns>true找到全部的指定物品，false至少有一个物品没有找到。</returns>
        public virtual bool LookupItems(GameChar gameChar, IReadOnlyDictionary<string, object> pb, string prefix,
            ICollection<(GameThingBase, IReadOnlyDictionary<string, object>)> data)
        {
            bool result = true;
            var coll = pb.GetValuesWithoutPrefix(prefix);
            var alls = gameChar.AllChildren.ToLookup(c => c.ExtraGuid); //所有物品
            foreach (var item in coll)
            {
                if (!OwConvert.TryToGuid(item.FirstOrDefault(c => c.Item1 == "tid").Item2, out var tid))  //若找不到tid
                    continue;
                GameItem gi;
                if (OwConvert.TryToGuid(item.FirstOrDefault(c => c.Item1 == "ptid").Item2, out var ptid))  //若指定了容器
                {
                    gi = alls[ptid].SelectMany(c => c.Children).FirstOrDefault(c => c.ExtraGuid == tid);
                }
                else //若没有指定容器
                {
                    gi = alls[tid].FirstOrDefault();
                }
                data.Add((gi, OwHelper.DictionaryFrom(item)));
                if (gi is null)
                    result = false;
            }
            return result;
        }

        /// <summary>
        /// 校验指定的对象是否符合属性集合中的要求。
        /// 当前仅校验数量要求是否足够。
        /// </summary>
        /// <param name="data">其中任何元素的Item1如果为null,都将导致立即返回false。</param>
        /// <returns></returns>
        public virtual bool Verify(IEnumerable<(GameThingBase, IReadOnlyDictionary<string, object>)> data)
        {
            //TODO:是否合并其他要求项，如特定对象特定属性的不等式。
            foreach (var item in data)  //校验代价
            {
                GameItem gi = item.Item1 as GameItem;
                var count = item.Item2.GetDecimalOrDefault("count");
                if (gi is null || gi.Count.GetValueOrDefault() < count)
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// 获取物品的默认容器,这与实际所属容器可能不同。
        /// </summary>
        /// <remarks><see cref="SimpleDynamicPropertyBase.Properties"/>中包含ptid（父容器模板Id）键值则优先使用。</remarks>
        /// <param name="gameItem"></param>
        /// <param name="gameChar"></param>
        /// <returns>默认容器对象，如果没有则返回null。</returns>
        public virtual GameThingBase GetDefaultContainer(GameItem gameItem, GameChar gameChar)
        {
            if (World.PropertyManager.TryGetPropertyWithTemplate(gameItem, "ptid", out var obj) && OwConvert.TryToGuid(obj, out var ptid))
                return gameChar.ExtraGuid == ptid ? gameChar as GameThingBase : gameChar.AllChildren.FirstOrDefault(c => c.ExtraGuid == ptid);
            return null;
        }

        /// <summary>
        /// 获取指定物品当前所属的直接父容器，当前版本可能是<see cref="GameItem"/>或<see cref="GameChar"/>,未来版本可能包含地图。
        /// </summary>
        /// <param name="gameItem"></param>
        /// <returns>返回父容器可能是另一个物品或角色对象，没有找到则返回null。</returns>
        /// <exception cref="ArgumentNullException"><paramref name="gameItem"/>是null。</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public virtual GameThingBase GetCurrentContainer(GameItem gameItem)
        {
            var result = gameItem.Parent as GameThingBase ?? (gameItem.OwnerId is null ? null : World.CharManager.GetCharFromId(gameItem.OwnerId.Value));
            return result;
        }

        /// <summary>
        /// 是否允许数量为0的物品继续存在。
        /// </summary>
        /// <param name="gItem"></param>
        /// <returns>对于货币带下对象返回true,否则返回false。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public virtual bool IsAllowZero(GameItem gItem)
        {
            if (ProjectConstant.CurrencyBagTId==gItem?.Parent?.ExtraGuid  )
                return true;
            return false;
        }

        #endregion 物品相关
    }

    public static class GameEventsManagerExtensions
    {
        #region 创建对象相关

        /// <summary>
        /// 初始化一个<see cref="GameItem"/>对象。
        /// </summary>
        /// <param name="mng"></param>
        /// <param name="gameItem"></param>
        /// <param name="template"></param>
        /// <param name="parent">父容器对象,如果为null，则使用<paramref name="ownerId"/>设置拥有者属性，否则忽略<paramref name="ownerId"/></param>
        /// <param name="ownerId"></param>
        /// <param name="parameters"></param>
        public static void GameItemCreated(this GameEventsManager mng, GameItem gameItem, [NotNull] GameItemTemplate template, [AllowNull] GameItem parent, Guid? ownerId,
            [AllowNull] IReadOnlyDictionary<string, object> parameters = null)
        {
            var bg = DictionaryPool<string, object>.Shared.Get();
            if (null != parameters)
                OwHelper.Copy(parameters, bg);
            if (ownerId.HasValue)
                bg["ownerid"] = ownerId.Value;
            else if (null != parent)
                bg["parent"] = parent;
            else if ((parent?.Id).HasValue)
                bg["ptid"] = parent?.Id;
            bg["tt"] = template;
            mng.GameItemCreated(gameItem, bg);
            DictionaryPool<string, object>.Shared.Return(bg);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void GameItemCreated(this GameEventsManager mng, GameItem gameItem, Guid templateId, [AllowNull] GameItem parent, Guid? ownerId,
            [AllowNull] IReadOnlyDictionary<string, object> parameters = null)
        {
            mng.GameItemCreated(gameItem, mng.World.ItemTemplateManager.GetTemplateFromeId(templateId), parent, ownerId, parameters);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void GameItemCreated(this GameEventsManager manager, GameItem gameItem, Guid templateId) =>
                   manager.GameItemCreated(gameItem, manager.World.ItemTemplateManager.GetTemplateFromeId(templateId));


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void GameItemCreated(this GameEventsManager manager, GameItem gameItem, GameItemTemplate template) =>
                    manager.GameItemCreated(gameItem, template, null, null);

        /// <summary>
        /// 在一个<see cref="GameThingBase"/>的子类被创建后调用。
        /// </summary>
        /// <param name="thing"></param>
        /// <param name="template"></param>
        /// <param name="propertyBag"></param>
        public static void GameThingCreated(this GameEventsManager mng, GameThingBase thing, GameThingTemplateBase template, IReadOnlyDictionary<string, object> propertyBag)
        {
            if (propertyBag.ContainsKey("tt") || template is null)
            {
                mng.GameThingCreated(thing, propertyBag);
            }
            else
            {
                var bg = DictionaryPool<string, object>.Shared.Get();
                OwHelper.Copy(propertyBag, bg);
                bg["tt"] = template;
                mng.GameThingCreated(thing, bg);
                DictionaryPool<string, object>.Shared.Return(bg);
            }
        }

        #endregion 创建对象相关

        #region 属性变化事件相关

        /// <summary>
        /// 设置属性并发送变化事件数据。
        /// </summary>
        /// <param name="gameChar"></param>
        /// <param name="obj"></param>
        /// <param name="name"></param>
        /// <param name="newValue"></param>
        /// <param name="tag">附属信息。</param>
        public static void PostDynamicPropertyChanged(this GameChar gameChar, SimpleDynamicPropertyBase obj, string name, object newValue, object tag)
        {
            var arg = GamePropertyChangeItemPool<object>.Shared.Get();
            arg.Object = obj; arg.PropertyName = name; arg.Tag = tag;
            if (obj.Properties.TryGetValue(name, out var oldValue))
            {
                arg.OldValue = oldValue;
                arg.HasOldValue = true;
            }
            obj.Properties[name] = newValue;
            arg.NewValue = newValue;
            arg.HasNewValue = true;
            gameChar.GetOrCreatePropertyChangedList().Enqueue(arg);
        }

        /// <summary>
        /// 删除指定属性。
        /// </summary>
        /// <param name="gameChar"></param>
        /// <param name="obj"></param>
        /// <param name="name"></param>
        /// <param name="tag"></param>
        public static void PostDynamicPropertyRemoved(this GameChar gameChar, SimpleDynamicPropertyBase obj, string name, object tag)
        {
            var arg = GamePropertyChangeItemPool<object>.Shared.Get();
            if (obj.Properties.Remove(name, out var oldValue))
            {
                arg.Object = obj; arg.PropertyName = name; arg.Tag = tag;
                arg.OldValue = oldValue;
                arg.HasOldValue = true;
                gameChar.GetOrCreatePropertyChangedList().Enqueue(arg);
            }
        }

        /// <summary>
        /// 将缓存的属性变化事件数据全部引发。
        /// </summary>
        /// <param name="manager"></param>
        /// <param name="gameChar"></param>
        public static bool DispatcherDynamicProperty(this GameEventsManager manager, GameChar gameChar)
        {
            List<Exception> excps = new List<Exception>();
            bool succ = false;
            var list = gameChar.GetOrCreatePropertyChangedList();
            GamePropertyChangeItem<object> item;
            while (!list.IsEmpty)    //若存在数据
            {
                for (var b = list.TryDequeue(out item); !b; b = list.TryDequeue(out item)) ;
                succ = true;
                try
                {
                    manager.OnPropertyChanged(item);
                }
                catch (Exception excp)
                {
                    excps.Add(excp);
                }
                GamePropertyChangeItemPool<object>.Shared.Return(item);  //放入池中备用
            }
            if (excps.Count > 0)    //若需要引发工程中堆积的异常
                throw new AggregateException(excps);
            return succ;
        }
        #endregion 属性变化事件相关

    }


}
