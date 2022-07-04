using Game.Social;
using GuangYuan.GY001.BLL;
using GuangYuan.GY001.BLL.Homeland;
using GuangYuan.GY001.TemplateDb;
using GuangYuan.GY001.UserDb;
using GuangYuan.GY001.UserDb.Social;
using Microsoft.EntityFrameworkCore;
using OW.Extensions.Game.Store;
using OW.Game.PropertyChange;
using OW.Game.Store;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;

namespace OW.Game.Item
{
    public class ItemIncrement : IDisposable
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="dic"></param>
        /// <param name="prefix">前缀，null表示无前缀。</param>
        /// <param name="result"></param>
        /// <returns></returns>
        public static bool TryParse(IReadOnlyDictionary<string, object> dic, [AllowNull] string prefix, out ItemIncrement result)
        {
            result = new ItemIncrement();
            var props = dic.GetValuesWithoutPrefix(prefix);
            var dics = props.Select(c => c.ToDictionary(c2 => c2.Item1, c2 => c2.Item2));
            result._Datas.AddRange(dics);
            return true;
        }

        /// <summary>
        /// 构造函数。
        /// </summary>
        public ItemIncrement()
        {

        }

        readonly List<IReadOnlyDictionary<string, object>> _Datas = new List<IReadOnlyDictionary<string, object>>();

        public List<IReadOnlyDictionary<string, object>> Datas => _Datas;

        //public static IEnumerable<GameItem> ToGameItems(this GameItemManager manager, IReadOnlyDictionary<string, object> bag, string prefix = null)
        //{
        //    var props = bag.GetValuesWithoutPrefix(prefix);
        //    var dics = props.Select(c => c.ToDictionary(c2 => c2.Item1, c2 => c2.Item2));
        //    var eventMng = manager.World.EventsManager;
        //    List<GameItem> result = new List<GameItem>();
        //    foreach (var item in dics)
        //    {
        //        if (!item.ContainsKey("tid") && !item.ContainsKey("tt"))    //若没有模板数据
        //            continue;
        //        if (!item.ContainsKey("tt") && item.GetGuidOrDefault("tid") == Guid.Empty)
        //            continue;
        //        var gi = new GameItem();
        //        eventMng.GameItemCreated(gi, item);
        //        result.Add(gi);
        //    }
        //    return result;
        //}

        #region IDisposable接口及相关

        private bool disposedValue;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // 释放托管状态(托管对象)
                }

                // 释放未托管的资源(未托管的对象)并重写终结器
                // 将大型字段设置为 null
                disposedValue = true;
            }
        }

        // 仅当“Dispose(bool disposing)”拥有用于释放未托管资源的代码时才替代终结器
        // ~ItemIncrement()
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

        #endregion IDisposable接口及相关
    }

    public class GameItemManagerOptions
    {
        public GameItemManagerOptions()
        {

        }

        /// <summary>
        /// 创建一个物品后调用此回调。
        /// </summary>
        public Func<IServiceProvider, GameItem, bool> ItemCreated { get; set; }
    }

    /// <summary>
    /// 虚拟物品管理器。
    /// </summary>
    public class GameItemManager : GameManagerBase<GameItemManagerOptions>
    {
        #region 构造函数

        public GameItemManager() : base()
        {
            Initialize();
        }

        public GameItemManager(IServiceProvider serviceProvider) : base(serviceProvider)
        {
            Initialize();
        }

        public GameItemManager(IServiceProvider serviceProvider, GameItemManagerOptions options) : base(serviceProvider, options)
        {
            Initialize();
        }

        void Initialize()
        {

        }
        #endregion 构造函数

        #region 属性及相关
        private GameItemTemplateManager _ItemTemplateManager;

        private GameItemTemplateManager ItemTemplateManager
        {
            get
            {
                if (_ItemTemplateManager is null)
                    Interlocked.CompareExchange(ref _ItemTemplateManager, World.ItemTemplateManager, null);
                return _ItemTemplateManager;
            }
        }
        #endregion

        /// <summary>
        /// 复位锁定槽中的道具，送回道具背包。
        /// </summary>
        /// <param name="gameChar"></param>
        public void ResetSlot(GameChar gameChar)
        {
            var gim = this;
            var daojuBag = gim.GetOrCreateItem(gameChar, ProjectConstant.DaojuBagSlotId);
            var slot = gameChar.GameItems.FirstOrDefault(c => c.TemplateId == ProjectConstant.LockAtkSlotId); //锁定槽
            gim.MoveItems(slot.Children, daojuBag);
            slot = gameChar.GameItems.FirstOrDefault(c => c.TemplateId == ProjectConstant.LockMhpSlotId); //锁定槽
            gim.MoveItems(slot.Children, daojuBag);
            slot = gameChar.GameItems.FirstOrDefault(c => c.TemplateId == ProjectConstant.LockQltSlotId); //锁定槽
            gim.MoveItems(slot.Children, daojuBag);
        }

        /// <summary>
        /// 刷新指定角色的木材堆叠上限属性。
        /// </summary>
        /// <param name="gameChar"></param>
        public void ComputeMucaiStc(GameChar gameChar)
        {
            var mucai = gameChar.GetMucai();
            var stc = mucai.GetTemplate().Properties.GetDecimalOrDefault("stc");
            var coll = gameChar.GetHomeland().GetAllChildren().Where(c => c.TemplateId == ProjectConstant.MucaiStoreTId);
            stc += coll.Sum(c => c.Properties.GetDecimalOrDefault("stc"));
            mucai.Properties["stc"] = stc;
        }

        /// <summary>
        /// 获取对象的模板。
        /// </summary>
        /// <param name="gameObject"></param>
        /// <returns>如果无效的模板Id，则返回null。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public GameItemTemplate GetTemplate(GameItemBase gameObject) =>
            gameObject.GetTemplate() as GameItemTemplate ?? ItemTemplateManager.GetTemplateFromeId(gameObject.TemplateId);

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <param name="tId"></param>
        /// <returns></returns>
        public GameItemTemplate GetTemplateFromeId(Guid tId)
        {
            return ItemTemplateManager.GetTemplateFromeId(tId);
        }

        #region 动态属性相关
        /// <summary>
        /// 设置动态属性。
        /// </summary>
        /// <param name="gameItem"></param>
        /// <param name="propName"></param>
        /// <param name="val"></param>
        /// <param name="gameChar"></param>
        /// <returns>true成功设置,false未能成功设置属性。</returns>
        public bool SetPropertyValue(GameItem gameItem, string propName, object val, GameChar gameChar = null)
        {
            gameItem.Properties.TryGetValue(propName, out var oldValue);
            //TO DO
            if (propName.Equals("tid", StringComparison.InvariantCultureIgnoreCase))
            {
                gameItem.TemplateId = (Guid)val;
            }
            else if (propName.Equals("pid", StringComparison.InvariantCultureIgnoreCase))
            {
                if (!OwConvert.TryToGuid(val, out var pid))
                    return false;
                gameItem.ParentId = pid;
            }
            else if (propName.Equals("ptid", StringComparison.InvariantCultureIgnoreCase))  //移动到新的父容器
            {
                var coll = OwHelper.GetAllSubItemsOfTree(new GameItem[] { gameItem }, c => c.Children);
                var tid = (Guid)val;
                var parent = coll.FirstOrDefault(c => c.TemplateId == tid); //获取目标容器
                var oldParent = World.EventsManager.GetCurrentContainer(gameItem); //现有容器
                if (null == oldParent) //若不是属于其他物品
                {
                    MoveItem(gameItem, gameItem.Count ?? 1, parent);
                }
                else
                {
                    var gis = World.PropertyManager.GetChildrenCollection(oldParent).Where(c => c.Id == gameItem.Id);
                    MoveItems(gis, parent);
                }
            }
            else if (propName.Equals("Count", StringComparison.InvariantCultureIgnoreCase))
            {
                var count = Convert.ToDecimal(val);
                if (count != 0)
                {
                    gameItem.Count = count;
                }
                else
                {
                    if (null != gameItem.Parent)
                        gameItem.Parent.Children.Remove(gameItem);
                    else if (null != gameItem.OwnerId) //若直属于角色
                    {
                        gameChar ??= World.CharManager.GetCharFromId(gameItem.OwnerId.Value);
                        if (null == gameChar)
                            return false;
                        gameChar.GameItems.Remove(gameItem);

                    }
                    else if (!(gameItem.ParentId is null))  //若是新加入物品
                    {
                        if (null == gameChar)
                            return false;
                        var tmp = OwHelper.GetAllSubItemsOfTree(gameChar.GameItems, c => c.Children).FirstOrDefault(c => c.Children.Contains(gameItem));
                        if (null == tmp)
                            return false;
                        tmp.Children.Remove(gameItem);
                    }
                    gameItem.Count = count;
                }

            }
            else if (propName.StartsWith(World.PropertyManager.LevelPropertyName))  //若是一个级别属性
            {
                var olv = gameItem.GetDecimalWithFcpOrDefault(propName, 0m);    //当前等级
                var nlv = Convert.ToDecimal(val);   //新等级
                if (olv != nlv)    //若需要改变等级
                {
                    string seqPName;
                    seqPName = propName.Length > 2 ? propName[2..] : World.PropertyManager.LevelPropertyName;
                    if (seqPName == World.PropertyManager.LevelPropertyName)
                        SetLevel(gameItem, (int)nlv);
                    else
                        SetLevel(gameItem, seqPName, (int)nlv);
                    gameItem.Properties[propName] = nlv;    //设置等级
                }
            }
            else if (propName.StartsWith(ProjectConstant.FastChangingPropertyName))  //若设置一个快速变化属性
            {
                if (propName.Length <= ProjectConstant.FastChangingPropertyName.Length)    //若名字太短
                    return false;
                string tmp = propName[ProjectConstant.FastChangingPropertyName.Length..];
                if (tmp.Length < 2)    //若名字太短
                    return false;
                var prefix = tmp[0];
                string innerName = tmp[1..];   //获得实际属性名
                if (!gameItem.Name2FastChangingProperty.TryGetValue(innerName, out var fcp))    //若不存在该属性
                {
                    fcp = new FastChangingProperty(default, default, default, default, DateTime.UtcNow);
                    gameItem.Name2FastChangingProperty[innerName] = fcp;
                }
                fcp.SetPropertyValue(prefix, val);
            }
            else
                gameItem.SetPropertyValue(propName, val);
            DynamicPropertyChangedCollection args = new DynamicPropertyChangedCollection();
            var item = new SimplePropertyChangedCollection() { Thing = gameItem };
            item.Add(new GamePropertyChangeItem<object>(null, name: propName, oldValue: oldValue, newValue: gameItem.Properties[propName]));
            args.Add(item);
            World.EventsManager.OnDynamicPropertyChanged(args);
            return true;
        }

        /// <summary>
        /// 将模板的属性与对象上的属性合并。添加没有的属性。
        /// </summary>
        /// <param name="gameItem"><see cref="GameObjectBase.TemplateId"/>属性必须正确设置。</param>
        /// <returns>true成功设置，false,找不到指定的模板。</returns>
        public bool MergeProperty(GameItem gameItem)
        {
            var template = ItemTemplateManager.GetTemplateFromeId(gameItem.TemplateId); //获取模板
            if (null == template)   //若找不到模板
                return false;
            var seqKeys = template.Properties.Where(c => c.Value is decimal[]).Select(c => (SeqPn: c.Key, IndexPn: ItemTemplateManager.GetIndexPropName(template, c.Key))).ToArray();    //序列属性的名字
            foreach (var (SeqPn, IndexPn) in seqKeys)   //设置序列属性
            {
                SetLevel(gameItem, SeqPn, Convert.ToInt32(gameItem.Properties.GetValueOrDefault(IndexPn, 0m)));
            }
            var keys = template.Properties.Where(c => !(c.Value is decimal[])).Select(c => c.Key).Except(gameItem.Properties.Keys).ToArray(); //需要增加的简单属性的名字
            foreach (var item in keys)  //添加简单属性
                gameItem.Properties[item] = template.Properties[item];
            return true;
        }

        /// <summary>
        /// 仅设置由lv控制的序列属性。
        /// 特别地，并不更改级别属性，调用者要自己更改。如lv并没有变化
        /// </summary>
        /// <param name="gameItem"></param>
        /// <param name="newLevel"></param>
        public void SetLevel(GameItem gameItem, int newLevel)
        {
            var template = GetTemplate(gameItem);
            var coll = template.Properties.Where(c => c.Value is decimal[] && World.ItemTemplateManager.GetIndexPropName(template, c.Key) == World.PropertyManager.LevelPropertyName);    //取得序列属性且其索引属性是通用序列的
            foreach (var item in coll.ToArray())
            {
                SetLevel(gameItem, item.Key, newLevel);
            }
        }

        /// <summary>
        /// 变换物品等级。会对比原等级的属性增减属性数值。如模板中原等级mhp=100,而物品mhp=120，则会用新等级mhp+20。
        /// 特别地，并不更改级别属性，调用者要自己更改。如lv并没有变化
        /// </summary>
        /// <param name="gameItem">要改变的对象。</param>
        /// <param name="seqPName">序列属性的名字。如果对象中没有索引必须的属性，则视同初始化属性。若无序列属性的值，但找到索引属性的话，则视同此属性值是模板中指定的值。</param>
        /// <param name="newLevel">新等级。</param>
        /// <returns>true成功设置，false没有找到指定级别的元素，通常是索引超限。</returns>
        /// <exception cref="ArgumentException">无法找到指定模板。</exception>
        public bool SetLevel(GameItem gameItem, string seqPName, int newLevel)
        {
            var template = GetTemplate(gameItem);
            if (null == template)   //若无法找到模板
                throw new ArgumentException($"无法找到指定模板(TemplateId={gameItem.TemplateId}),对象Id={gameItem.Id}", nameof(gameItem));
            if (!template.Properties.TryGetValue(seqPName, out object objSeq) || !(objSeq is decimal[] seq))
                throw new ArgumentOutOfRangeException($"模板{template.Id}({template.DisplayName})中没有指定 {seqPName} 属性，或其不是序列属性");
            var indexPN = ItemTemplateManager.GetIndexPropName(template, seqPName); //索引属性的名字

            if (!gameItem.Properties.TryGetValue(indexPN, out object objLv))  //若没有指定当前等级
            {
                //当前视同需要初始化属性
                gameItem.Properties[seqPName] = seq[newLevel];
            }
            else
            {
                var lv = Convert.ToInt32(objLv);   //当前等级
                if (lv >= seq.Length || lv < 0) //若等级超过限制
                {
                    //gameItem.Properties.Remove(seqPName);
                    return false;
                }
                var oov = seq[lv];  //原级别模板值

                var val = Convert.ToDecimal(gameItem.Properties.GetValueOrDefault(seqPName, oov));  //物品的属性值
                var old = newLevel < seq.Length ? seq[newLevel] : oov;  //可能缺失最后一级数据
                gameItem.Properties[seqPName] = old + val - oov; //TO DO缺少对快速变化属性的同步
            }
            return true;
        }

        /// <summary>
        /// 设置新等级并对动态属性增减。
        /// </summary>
        /// <param name="thing">目标对象属性字典。</param>
        /// <param name="newLevel">新等级。</param>
        /// <param name="changes">变化数据。省略则不生成。</param>
        /// 
        public void SetLevel(GameThingBase thing, int newLevel, ICollection<GamePropertyChangeItem<object>> changes = null)
        {
            var olv = (int)thing.GetDecimalWithFcpOrDefault(World.PropertyManager.LevelPropertyName); //当前等级
            if (olv == newLevel)    //若等级没有变化
                return;
            var tt = thing.GetTemplate();
            foreach (var kvp in thing.Properties.ToArray())
            {
                var ary = tt.Properties.GetValueOrDefault(kvp.Key) as decimal[];
                if (ary is null || ary.Length < 1)  //若没有随级别变化的可能
                    continue;
                if (!OwConvert.TryToDecimal(kvp.Value, out var ov)) //若不是数值
                    continue;

                var result = ov - GetOrDefault(ary, olv) + GetOrDefault(ary, newLevel);   //升级后的值
                thing.Properties[kvp.Key] = result;
                changes?.Add(new GamePropertyChangeItem<object>()
                {
                    HasNewValue = true,
                    NewValue = result,
                    HasOldValue = true,
                    OldValue = ov,
                    Object = thing,
                    PropertyName = kvp.Key,
                });
            }
            //设置等级属性
            thing.Properties[World.PropertyManager.LevelPropertyName] = newLevel;
            changes?.Add(new GamePropertyChangeItem<object>()
            {
                HasNewValue = true,
                NewValue = newLevel,
                HasOldValue = true,
                OldValue = olv,
                Object = thing,
                PropertyName = World.PropertyManager.LevelPropertyName,
            });
        }

        /// <summary>
        /// 获取数组指定索引处的值，若索引超出范围则返回默认值。
        /// </summary>
        /// <param name="ary"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        T GetOrDefault<T>(T[] ary, int index, T defaultValue = default) =>
            index < ary.GetLowerBound(0) || index > ary.GetUpperBound(0) ? defaultValue : ary[index];

        #endregion 动态属性相关

        /// <summary>
        /// 标准化物品，避免有后增加的槽没有放置上去。
        /// </summary>
        public void Normalize(IEnumerable<GameItem> gameItems)
        {
            var gitm = World.ItemTemplateManager;
            var coll = (from tmp in OwHelper.GetAllSubItemsOfTree(gameItems, c => c.Children)
                        let tt = gitm.GetTemplateFromeId(tmp.TemplateId)
                        select (tmp, tt)).ToArray();
            var gim = World.ItemManager;
            List<Guid> adds = new List<Guid>();
            foreach (var (tmp, tt) in coll)
            {
                tmp.GenerateIdIfEmpty();
                tmp.SetTemplate(tt);
                adds.Clear();
                tmp.Children.ApartWithWithRepeated(tt.ChildrenTemplateIds, c => c.TemplateId, c => c, null, null, adds);
                foreach (var addItem in adds)
                {
                    var newItem = new GameItem();
                    World.EventsManager.GameItemCreated(newItem, addItem, tmp, null);
                    tmp.Children.Add(newItem);
                }
            }
        }

        /// <summary>
        /// 获取指定阵容的所有坐骑。
        /// </summary>
        /// <param name="number"></param>
        /// <returns></returns>
        public IEnumerable<GameItem> GetLineup(GameChar gameChar, int number)
        {
            var bag = gameChar.GetZuojiBag();
            var coll = bag.Children.Where(c =>
            {
                return c.Properties.Keys.Any(c1 =>
                {
                    if (!c1.StartsWith(ProjectConstant.ZhenrongPropertyName) || c1.Length <= ProjectConstant.ZhenrongPropertyName.Length) return false;
                    var index = ProjectConstant.ZhenrongPropertyName.Length;
                    if (!int.TryParse(c1[index..], out var num)) return false;
                    return num == number;
                });
            });
            return coll;
        }

        /// <summary>
        /// 卖出物品。
        /// </summary>
        /// <param name="datas">工作参数及返回值封装类。<seealso cref="SellDatas"/></param>
        public void Sell(SellDatas datas)
        {
            using var disposer = datas.LockUser();
            if (disposer is null) return;
            var gc = datas.GameChar;
            var shoulan = gc.GetShoulanBag();
            if (datas.SellIds.Select(c => c.Item1).Distinct().Count() != datas.SellIds.Count)
            {
                datas.ErrorCode = (int)HttpStatusCode.BadRequest;
                datas.ErrorMessage = "物品Id重复。";
                datas.HasError = true;
                return;
            }
            var coll = (from sellItem in datas.SellIds
                        join gi in datas.GameChar.AllChildren
                        on sellItem.Item1 equals gi.Id
                        select (gi, sellItem.Item2));

            List<(GameItem, decimal, decimal, decimal)> list = new List<(GameItem, decimal, decimal, decimal)>();
            var totalGold = 0m; var totalDia = 0m;  //总计价格
            foreach (var item in coll)
            {
                if (ComputeGoldPrice(item.gi, out var gold, out var dia))
                {
                    totalGold += gold * item.Item2;
                    totalDia += dia * item.Item2;
                    list.Add((item.gi, item.Item2, dia, gold));
                }
            }
            if (list.Count != datas.SellIds.Count)
            {
                datas.ErrorCode = (int)HttpStatusCode.BadRequest;
                datas.ErrorMessage = "至少一个指定的Id不存在或不能出售。";
                datas.HasError = true;
                return;
            }
            var gim = World.ItemManager;
            var qiwu = datas.GameChar.GetQiwuBag(); //回收站

            //改写物品对象
            foreach (var item in list)
            {
                gim.MoveItem(item.Item1, item.Item2, qiwu, null, datas.PropertyChanges);
            }
            //改写金币
            if (totalGold != 0)
            {
                var jinbi = datas.GameChar.GetJinbi();
                jinbi.Count += totalGold;
                datas.ChangeItems.AddToChanges(jinbi);

            }
            //改写钻石
            if (totalDia != 0)
            {
                var zuanshi = datas.GameChar.GetZuanshi();
                zuanshi.Count += totalDia;
                datas.ChangeItems.AddToChanges(zuanshi);
            }
            datas.PropertyChanges.CopyTo(datas.ChangeItems);
        }

        /// <summary>
        /// 计算某个物品的金币售价。
        /// </summary>
        /// <param name="gold">金币售价。</param>
        /// <param name="dia">钻石售价。</param>
        /// <returns>true表示可以出售，false表示不可出售物品。</returns>
        public bool ComputeGoldPrice(GameItem item, out decimal gold, out decimal dia)
        {
            if (this.IsMounts(item))
            {
                gold = this.GetBody(item).Properties.GetDecimalOrDefault("sg");
                var totalNe = item.Properties.GetDecimalOrDefault("neatk", 0m) +   //总资质值
                    item.Properties.GetDecimalOrDefault("nemhp", 0m) +
                    item.Properties.GetDecimalOrDefault("neqlt", 0m);
                totalNe = Math.Round(totalNe, MidpointRounding.AwayFromZero);  //取整，容错
                decimal mul;

                if (totalNe >= 0 && totalNe <= 60) mul = 1;
                else if (totalNe >= 61 && totalNe <= 120) mul = 1.5m;
                else if (totalNe >= 121 && totalNe <= 180) mul = 2;
                else if (totalNe >= 181 && totalNe <= 240) mul = 3;
                else if (totalNe >= 241 && totalNe <= 300) mul = 4;
                else mul = 0;
                gold = mul * gold;
                dia = 0;
            }
            else
            {
                gold = item.GetDecimalWithFcpOrDefault("sg");
                dia = item.GetDecimalWithFcpOrDefault("sd");
            }
            return true;
        }

        /// <summary>
        /// 设置阵容号，或取消阵容设置。
        /// </summary>
        /// <param name="datas"><see cref="SetLineupDatas"/></param>
        public void SetLineup(SetLineupDatas datas)
        {
            using var disposer = datas.LockUser();
            if (disposer is null)
                return;

            var gc = datas.GameChar;
            var srs = new HashSet<Guid>(gc.GetZuojiBag().Children.Select(c => c.Id));
            if (!srs.IsSupersetOf(datas.Settings.Select(c => c.Item1)))
            {
                datas.ErrorMessage = "至少一个指定的坐骑Id不存在。";
                datas.HasError = true;
                datas.ErrorCode = (int)HttpStatusCode.BadRequest;
                return;
            }
            var pchange = new DynamicPropertyChangedCollection();   //属性变化数据类
            var db = gc.GameUser.DbContext;
            foreach (var item in datas.Settings)    //逐个设置
            {
                var mounts = gc.GetMounetsFromId(item.Item1);
                var key = $"{ProjectConstant.ZhenrongPropertyName}{item.Item2}";
                if (item.Item3 != -1)  //若设置阵容
                {
                    //mounts.Properties[key] = item.Item3;
                    pchange.MarkAndSet(mounts, key, item.Item3);
                    if (item.Item2 == 10)  //若是家园展示
                    {
                        var tid = World.ItemManager.GetBody(mounts).TemplateId; //身体的模板Id
                        var sr = db.Set<GameSocialRelationship>().Find(gc.Id, tid, SocialConstant.HomelandShowKeyType);
                        if (sr is null)
                        {
                            sr = new GameSocialRelationship
                            {
                                Id = gc.Id,
                                Id2 = tid,
                                KeyType = SocialConstant.HomelandShowKeyType,
                            };
                            db.Add(sr);
                        }
                    }
                }
                else //若取消阵容设置
                {
                    //mounts.Properties.Remove(key);
                    pchange.MarkAndRemove(mounts, key);
                    if (item.Item2 == 10)  //若是家园展示
                    {
                        var tid = World.ItemManager.GetBody(mounts).TemplateId; //身体的模板Id
                        var sr = db.Set<GameSocialRelationship>().Find(gc.Id, tid, SocialConstant.HomelandShowKeyType);
                        if (null != sr)
                        {
                            db.Remove(sr);
                        }
                    }
                }
                datas.ChangeItems.AddToChanges(mounts);
            }
            World.EventsManager.OnDynamicPropertyChanged(pchange);
            World.CharManager.NotifyChange(gc.GameUser);
        }

        /// <summary>
        /// 使用道具。
        /// </summary>
        public void UseItems(UseItemsWorkDatas datas)
        {
            using var dwChar = datas.LockUser();
            if (dwChar is null)
            {
                datas.ErrorCode = (int)HttpStatusCode.Unauthorized;
                datas.HasError = true;
                return;
            }
            GameItem gi = datas.Item.Item1;
            List<GamePropertyChangeItem<object>> changes = new List<GamePropertyChangeItem<object>>();
            if (!UseItem(gi, datas.Item.Item2, datas.Remainder, changes))
            {
                datas.ErrorCode = VWorld.GetLastError();
                datas.ErrorMessage = VWorld.GetLastErrorMessage();
                datas.HasError = true;
            }
            else
            {
                datas.SuccCount = (int)datas.Item.Item2;
                changes.CopyTo(datas.ChangeItems);
                ChangeItem.Reduce(datas.ChangeItems);
            }
            if (datas.ChangeItems.Count > 0)
                World.CharManager.NotifyChange(datas.GameChar.GameUser);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="datas"></param>
        public void GetRankOfTuiguan(GetRankOfTuiguanDatas datas)
        {
            using var dwUser = datas.LockUser();
            if (dwUser is null)
                return;
            var db = datas.UserDbContext;
            var tuiguanObj = datas.GameChar.GetTuiguanObject();
            var coll = from slot in db.Set<GameItem>()
                       where slot.TemplateId == ProjectConstant.TuiGuanTId
                       join parent in db.Set<GameItem>()
                       on slot.ParentId equals parent.Id
                       join gc in db.Set<GameChar>()
                       on parent.OwnerId equals gc.Id
                       select new { gc.Id, gc.DisplayName, slot.ExtraDecimal.Value, gc.PropertiesString };
            //gc.Properties.GetDecimalOrDefault("charIcon", 0)
            var gChar = datas.GameChar;
            var coll1 = from tmp in coll.AsNoTracking()    //排名在当前角色之前的角色
                        where (tmp.Value > tuiguanObj.ExtraDecimal.Value || tmp.Value == tuiguanObj.ExtraDecimal.Value && string.Compare(tmp.DisplayName, gChar.DisplayName) < 0)
                        orderby tmp.Value, tmp.DisplayName
                        select tmp;
            var rank = coll.Count();
            datas.Rank = rank;
            datas.Scope = tuiguanObj.ExtraDecimal.Value;
            var prv = coll.Take(25).ToList();   //排在前面的的紧邻数据
            datas.Prv.AddRange(prv.Select(c =>
            {
                var tmp = new Dictionary<string, object>();
                OwConvert.Copy(c.PropertiesString, tmp);
                var icon = tmp.GetDecimalOrDefault("charIcon", 0);
                return (c.Id, c.Value, c.DisplayName, icon);
            }));

            var collNext = from tmp in coll    //排在指定角色之后的
                           where (tmp.Value < tuiguanObj.ExtraDecimal.Value || tmp.Value == tuiguanObj.ExtraDecimal && string.Compare(tmp.DisplayName, gChar.DisplayName) > 0)
                           orderby tmp.Value descending, tmp.DisplayName descending
                           select tmp;
            var next = collNext.Take(25).ToList();
            datas.Next.AddRange(next.Select(c =>
            {
                var tmp = new Dictionary<string, object>();
                OwConvert.Copy(c.PropertiesString, tmp);
                var icon = tmp.GetDecimalOrDefault("charIcon", 0);
                return (c.Id, c.Value, c.DisplayName, icon);
            }));
        }

        /// <summary>
        /// 追加物品，如果无法放入则发送邮件邮寄。
        /// </summary>
        /// <param name="datas"></param>
        public void AddItemsOrMail(AddItemsOrMailDatas datas)
        {
            using var dwUser = datas.LockUser();
            if (dwUser is null)
                return;
            var container = datas.GameChar.AllChildren.ToLookup(c => c.TemplateId);

            List<GameItem> re = new List<GameItem>();
            var coll = (from tmp in datas.Items
                        group tmp.Item1 by tmp.Item2 into g
                        let parent = container[g.Key].FirstOrDefault()
                        where parent != null
                        select (g, parent)).ToList();

            foreach (var (g, parent) in coll)
            {
                foreach (var item in g)
                {
                    if (parent.TemplateId == ProjectConstant.ZuojiBagSlotId && this.IsMounts(item) && this.IsExistsMounts(item, datas.GameChar))   //若向坐骑背包放入重复坐骑
                        MoveItem(item, item.Count ?? 1, datas.GameChar.GetShoulanBag(), re, datas.PropertyChanges);
                    else
                        MoveItem(item, item.Count ?? 1, parent, re, datas.PropertyChanges);
                }
            }
            if (re.Count > 0)  //若需要发送邮件
            {
                var mail = new GameMail();
                World.SocialManager.SendMail(mail, new Guid[] { datas.GameChar.Id }, SocialConstant.FromSystemId, re.Select(c => (c, World.EventsManager.GetDefaultContainer(c, datas.GameChar).TemplateId)));
            }
            datas.PropertyChanges.CopyTo(datas.ChangeItems);
        }

        /// <summary>
        /// 设置数量属性，若考虑自动删除对象等事项。
        /// </summary>
        /// <param name="gameItem"></param>
        /// <param name="count">只能是非负数。若设置为0则根据<see cref="GameEventsManager.IsAllowZero(GameItem)"/>决定是否删除对象。</param>
        /// <param name="changes">变化数据，可以是空表示不记录变化数据。</param>
        public virtual bool ForcedSetCount(GameItem gameItem, decimal count, [AllowNull] ICollection<ChangeItem> changes = null)
        {
            gameItem.Count = count;
            if (gameItem.Parent is null)    //若设置的是游离对象
            {

            }
            else if (decimal.Zero == gameItem.Count)   //若已经变为0
            {
                if (!World.PropertyManager.IsStc(gameItem, out _) || gameItem.Parent?.TemplateId != ProjectConstant.CurrencyBagTId)   //若应删除对象
                {
                    var pid = gameItem.ParentId ?? gameItem.OwnerId.Value;
                    if (!ForcedDelete(gameItem)) //若无法删除
                        return false;
                    changes?.AddToRemoves(pid, gameItem.Id);
                }
                else //不用删除对象
                {
                    changes?.AddToChanges(gameItem);
                }
            }
            else //非0
            {
                changes?.AddToChanges(gameItem);
            }
            return true;

        }

        #region 物品操作

        /// <summary>
        /// 创建或获取指定模板id的孩子对象。
        /// </summary>
        /// <param name="parent">直接双亲容器。</param>
        /// <param name="tid"></param>
        /// <param name="creator"></param>
        /// <returns></returns>
        public virtual GameItem GetOrCreateItem(GameThingBase parent, Guid tid, [AllowNull] Action<GameItem> creator = null)
        {
            var child = World.PropertyManager.GetChildrenCollection(parent);
            var result = child.FirstOrDefault(c => c.TemplateId == tid);
            if (result is null) //若需要创建
            {
                result = new GameItem();
                child.Add(result);
                World.EventsManager.GameItemCreated(result, tid);
                creator?.Invoke(result);
                if (parent is GameChar gc)  //若添加到角色的直接对象
                {
                    gc.GetDbContext().Add(result);
                    result.OwnerId = gc.Id;
                }
            }
            return result;
        }
        #region 基本操作

        /// <summary>
        /// 移动指定物品的指定数量到指定容器。
        /// 不考虑已存在的可堆叠物品合并，不考虑容量和堆叠限制。
        /// </summary>
        /// <param name="gItem">要移动的对象。不可堆叠物品或全部移动，则更改物品的父容器。如果部分移动则生成新对象加入目标容器，此参数指定对象仅更改数量，
        /// 此时不移动包含的子对象。</param>
        /// <param name="count">要移动的数量，不能大于<paramref name="gItem"/>已有的数量。对不可堆叠物品应该是1。</param>
        /// <param name="container">目标容器。</param>
        /// <param name="changes">变化数据。</param>
        /// <returns></returns>
        public virtual void ForcedMove([NotNull] GameItem gItem, decimal count, [NotNull] GameThingBase container,
            [AllowNull] ICollection<GamePropertyChangeItem<object>> changes = null)
        {
            var propertyManager = World.PropertyManager;
            if (!propertyManager.IsStc(gItem, out _) && 1 != count)
                throw new ArgumentException("不可堆叠物品数量必须是1。", nameof(count));
            if (gItem.Count.Value == count || !propertyManager.IsStc(gItem, out _))    //若全部移动
            {
                ForcedRemove(gItem, changes); //确保解除原有的拥有关系
                ForcedAdd(gItem, container, changes);
            }
            else if (gItem.Count.Value > count)//若部分移动
            {
                var gi = new GameItem();
                World.EventsManager.GameItemCreated(gi, gItem.TemplateId);
                gi.Count = count;
                ForcedSetCount(gItem, gItem.Count.Value - count, changes);
                ForcedAdd(gi, container, changes);
            }
            else
                throw new ArgumentOutOfRangeException(nameof(count), "必须小于或等于物品的现存数量。");
        }

        /// <summary>
        /// 无视容量限制堆叠规则。将物品加入指定容器。无视物品的当前容器。也不考虑已存在的可合并项。
        /// </summary>
        /// <param name="gameItem">无视容量限制堆叠规则，不考虑原有容器。</param>
        /// <param name="container">无视容量限制堆叠规则。</param>
        /// <param name="changes">记录变化的集合，省略或为null则忽略。</param>
        /// <returns>true成功加入.
        /// false <paramref name="container"/>不是可以容纳物品的类型,这里仅指对象的类型无法识别（既非<see cref="GameItem"/>也非<see cref="GameChar"/>），而不会校验容量。
        /// </returns>
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public virtual bool ForcedAdd([NotNull] GameItem gameItem, [NotNull] GameThingBase container, [AllowNull] ICollection<GamePropertyChangeItem<object>> changes = null)
        {
            if (container is GameChar gChar)  //若容器是角色
            {
                gameItem.GenerateIdIfEmpty();
                gChar.GameItems.Add(gameItem);
                gameItem.OwnerId = gChar.Id;
            }
            else if (container is GameItem gItem)   //若容器是物品
            {
                gameItem.GenerateIdIfEmpty();
                gItem.Children.Add(gameItem);
                gameItem.ParentId = gItem.Id;
                gameItem.Parent = gItem;
            }
            else //不认识容器的种类
                return false;
            changes?.MarkAddChildren(container, gameItem);
            return true;
        }

        /// <summary>
        /// 获取连个物品是否可以合并。可以合并不考虑堆叠限制因素，仅说明是同类型的可堆叠物品。
        /// </summary>
        /// <param name="src"></param>
        /// <param name="dest"></param>
        /// <returns>true可以合并，false不可以合并。</returns>
        [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
        public virtual bool IsAllowMerge(GameItem src, GameItem dest) =>
            src.TemplateId != dest.TemplateId || !World.PropertyManager.IsStc(src, out _) || !World.PropertyManager.IsStc(dest, out _) ? false : true;

        /// <summary>
        /// 计算可移动的数量。不考虑是否是同类型物品。
        /// </summary>
        /// <param name="src">减少数量的物品。</param>
        /// <param name="dest">增加数量的物品。</param>
        /// <param name="maxCount">从源中最多移走多少，null或省略则不限定最多数量(等同于源物品的数量)。</param>
        /// <returns>src中减少并加入dest中的数量。对于不可合并物品(不同物品或不可堆叠物品)立即返回0，</returns>
        [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
        public virtual decimal GetCountOfMergeable(GameItem src, GameItem dest, decimal? maxCount = null)
        {
            var stc = World.PropertyManager.GetRemainderStc(dest);  //堆叠还剩余多少数量
            stc = Math.Min(stc, src.Count.Value);
            return maxCount.HasValue ? Math.Min(stc, maxCount.Value) : stc;
        }


        /// <summary>
        /// 强制将一个物品从它现有容器中移除。仅断开其关系，而不删除对象。
        /// </summary>
        /// <param name="gameItem"></param>
        /// <param name="changes"></param>
        /// <returns>true成功移除，false物品当前没有容器,此时没有变化数据。</returns>
        /// <exception cref="ArgumentNullException"><paramref name="gameItem"/>是null。</exception>
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public virtual bool ForcedRemove(GameItem gameItem, [AllowNull] ICollection<GamePropertyChangeItem<object>> changes = null)
        {
            bool result;
            var container = World.EventsManager.GetCurrentContainer(gameItem);
            if (container is null)  //若无容器
                return false;
            else //若有现有容器
                result = World.PropertyManager.GetChildrenCollection(container)?.Remove(gameItem) ?? false;
            if (result)  //若修改数据成功
            {
                gameItem.Parent = null; gameItem.ParentId = gameItem.OwnerId = null;
                changes?.MarkRemoveChildren(container, gameItem);
            }
            return result;
        }

        /// <summary>
        /// 强制彻底删除一个物品。会正确设置导航属性。
        /// </summary>
        /// <param name="gameItem"></param>
        /// <param name="db">使用的数据库上下文，如果省略或为null则会在关系中寻找。若找不到则不会在数据库中彻底删除对象，仅移除关系，这将导致孤立对象。</param>
        /// <param name="changes"></param>
        /// <returns>true成功删除，false,指定对象本不在数据库中。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual bool ForcedDelete(GameItem gameItem, DbContext db = null, [AllowNull] ICollection<GamePropertyChangeItem<object>> changes = null)
        {
            bool result = ForcedRemove(gameItem, changes);
            if (result)   //若成功移除关系
            {
                db ??= gameItem.GetDbContext();
                if (null != db && db.Entry(gameItem).State != EntityState.Detached)  //若非新加入的物品
                    db.Remove(gameItem);
            }
            return result;
        }

        /// <summary>
        /// 强制增减物品数量。从源中减少数量并在目标中增加同样数量。
        /// </summary>
        /// <param name="src">并入的物品，若全部并入则根据<see cref="GameEventsManager.IsAllowZero(GameItem)"/>决定是否删除物品。</param>
        /// <param name="dest"></param>
        /// <param name="count">要增减的数量。0则立即返回false。省略或为null则视同是<paramref name="src"/>的全部数量。</param>
        /// <param name="changes">详细的变化数据。</param>
        /// <returns>true合并成功，false物品不同模板属性或不是可堆叠物品。</returns>
        public virtual bool ForcedSetCount(GameItem src, GameItem dest, decimal? count = null, [AllowNull] ICollection<GamePropertyChangeItem<object>> changes = null)
        {
            if (count == decimal.Zero) //若无需合并
                return false;
            count ??= src.Count;
            if (count > src.Count)   //非法参数
                throw new ArgumentOutOfRangeException(nameof(count), "必须小于或等于物品的实际数量。");

            ForcedSetCount(src, src.Count.Value - count.Value, changes);
            ForcedSetCount(dest, dest.Count.Value + count.Value, changes);
            return true;
        }

        /// <summary>
        /// 设置物品对象的数量，若设置为0则根据设置（<see cref="GameEventsManager.IsAllowZero(GameItem)"/>）决定是否删除对象。
        /// </summary>
        /// <param name="gItem"></param>
        /// <param name="count"></param>
        /// <param name="changes"></param>
        /// <returns></returns>
        public virtual bool ForcedSetCount([NotNull] GameItem gItem, decimal count, [AllowNull] ICollection<GamePropertyChangeItem<object>> changes = null)
        {
            if (gItem.Parent is null && gItem.OwnerId is null)    //若设置的是游离对象
            {
                gItem.SetPropertyAndReturnChangedItem(World.PropertyManager.CountPropertyName, count, null, changes);
            }
            else if (decimal.Zero == count)   //若已经变为0
            {
                if (!World.EventsManager.IsAllowZero(gItem))   //若应删除对象
                {
                    return ForcedDelete(gItem, null, changes); //若无法删除
                }
                else //不用删除对象
                {
                    gItem.SetPropertyAndReturnChangedItem(World.PropertyManager.CountPropertyName, count, null, changes);
                }
            }
            else //若设置非0值
            {
                gItem.SetPropertyAndReturnChangedItem(World.PropertyManager.CountPropertyName, count, null, changes);
            }
            return true;

        }

        #endregion 基本操作

        /// <summary>
        /// 将物品全部加入到指定角色中。容器使用<see cref="GameEventsManager.GetDefaultContainer(GameItem, GameChar)"/>获取。
        /// </summary>
        /// <param name="gameChar"></param>
        /// <param name="gItems"></param>
        /// <param name="remainder"></param>
        /// <param name="changes"></param>
        public virtual void AddItems(GameChar gameChar, IEnumerable<GameItem> gItems, [AllowNull] ICollection<GameItem> remainder = null,
            [AllowNull] ICollection<GamePropertyChangeItem<object>> changes = null)
        {
            foreach (var item in gItems)
            {
                var container = World.EventsManager.GetDefaultContainer(item, gameChar);
                MoveItem(item, item.Count.Value, container, remainder, changes);
            }
        }

        /// <summary>
        /// 按属性包内指定的条件寻找指定的物品。条件不满足，数量不足的不会返回在结果中。
        /// </summary>
        /// <param name="gameChar"></param>
        /// <param name="propertyBag"></param>
        /// <param name="prefix"></param>
        /// <returns>物品和要求数量的集合。</returns>
        public virtual List<(GameItem, decimal)> Lookup(GameChar gameChar, IReadOnlyDictionary<string, object> propertyBag, string prefix = null)
        {
            var coll = propertyBag.GetValuesWithoutPrefix(prefix);
            var tid2gi = gameChar.AllChildren.ToLookup(c => c.TemplateId);
            var result = new List<(GameItem, decimal)>();
            foreach (var item in coll)
            {
                var tidVt = item.FirstOrDefault(c => c.Item1 == "tid");
                if (!OwConvert.TryToGuid(tidVt.Item2, out var tid)) //若没有模板id
                    continue;
                var countVt = item.FirstOrDefault(c => c.Item1 == "count");
                if (countVt.Item1 != "count" || !OwConvert.TryToDecimal(countVt.Item2, out var count))   //若没有指定数量
                    continue;
                var ptidVt = item.FirstOrDefault(c => c.Item1 == "ptid");
                GameItem gi;
                if (OwConvert.TryToGuid(ptidVt.Item2, out var ptid))  //若限定容器
                {
                    gi = tid2gi[ptid].SelectMany(c => c.Children).FirstOrDefault(c => c.TemplateId == tid);
                }
                else //若未限定容器
                {
                    gi = tid2gi[tid].FirstOrDefault();
                }
                if (null != gi)
                    result.Add((gi, count));
            }
            return result;
        }

        /// <summary>
        /// 消耗资源。
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="changes">元素是指定的对象和要消耗的数量。</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void DecrementCount(IEnumerable<(GameItem, decimal)> obj, ICollection<GamePropertyChangeItem<object>> changes = null)
        {
            foreach (var item in obj)
                this.ForcedAddCount(item.Item1, -Math.Abs(item.Item2), changes);
        }

        /// <summary>
        /// 按字典指定的物品消耗资源。
        /// </summary>
        /// <param name="gameChar"></param>
        /// <param name="propertyBag"></param>
        /// <param name="prefix"></param>
        /// <param name="changes"></param>
        /// <returns>true减少了所有材料，false至少有一种材料缺失。</returns>
        public virtual bool DecrementCount(GameChar gameChar, IReadOnlyDictionary<string, object> propertyBag, string prefix = null,
            ICollection<GamePropertyChangeItem<object>> changes = null)
        {
            var list = Lookup(gameChar, propertyBag, prefix);
            var cost = propertyBag.GetValuesWithoutPrefix(prefix);
            var count = cost.Count();
            if (list.Count < count)    //若资源不足
            {
                VWorld.SetLastErrorMessage($"缺少材料种类不齐。");
                VWorld.SetLastError(ErrorCodes.RPC_S_OUT_OF_RESOURCES);
                return false;
            }
            if (list.Count > 0)
            {
                var errItem = list.FirstOrDefault(c => c.Item1.Count.Value < c.Item2);
                if (errItem.Item1 != null)    //若至少有一个材料不够
                {
                    VWorld.SetLastErrorMessage($"至少有一个材料不够。{errItem.Item1.GetTemplate().DisplayName}需要{errItem.Item2},但只有{errItem.Item1.Count}");
                    VWorld.SetLastError(ErrorCodes.RPC_S_OUT_OF_RESOURCES);
                    return false;
                }
                DecrementCount(list, changes);
            }
            return true;
        }

        /// <summary>
        /// 校验数量书否足够。
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public virtual bool Verify(IEnumerable<(GameItem, decimal)> obj)
        {
            var first = obj.FirstOrDefault(c => c.Item1 is null || c.Item1.Count.GetValueOrDefault() < c.Item2);
            return true;
        }

        /// <summary>
        /// 尽可能将指定物品放入容器(可合并则合并)，如果有剩余则放入<paramref name="remainder"/>中。
        /// </summary>
        /// <param name="gItem"></param>
        /// <param name="count">移动的数量，不能大于物品已有数量。不可堆叠物品则必须是1。</param>
        /// <param name="container"></param>
        /// <param name="remainder"></param>
        /// <param name="changes"></param>
        public virtual void MoveItem(GameItem gItem, decimal count, GameThingBase container, [AllowNull] ICollection<GameItem> remainder = null,
            [AllowNull] ICollection<GamePropertyChangeItem<object>> changes = null)
        {
            var propMng = World.PropertyManager;
            if (propMng.IsStc(gItem, out _)) //若可堆叠
            {
                if (count > gItem.Count.Value)
                    throw new ArgumentOutOfRangeException(nameof(count), "必须小于或等于物品的实际数量。");
                var children = propMng.GetChildrenCollection(container);    //子容器
                var gi = children.FirstOrDefault(c => c.TemplateId == gItem.TemplateId);    //已存在的同类物品
                if (gi is null)  //若不存在同类物品
                {
                    var rCap = propMng.GetRemainderCap(container);
                    if (rCap < 1) //若不可容纳
                    {
                        VWorld.SetLastError(ErrorCodes.ERROR_IMPLEMENTATION_LIMIT);
                        remainder?.Add(gItem);
                    }
                    else //若可以容纳
                    {
                        if (count < gItem.Count.Value) //若不全部移动
                            remainder?.Add(gItem);
                        ForcedMove(gItem, count, container, changes);
                    }
                }
                else //若存在同类物品
                {
                    var countMove = GetCountOfMergeable(gItem, gi, count);  //实际移动数量
                    if (countMove < gItem.Count)    //若部分移动
                        remainder?.Add(gItem);
                    ForcedSetCount(gItem, gi, countMove, changes);
                }
            }
            else //若不可堆叠
            {
                var rCap = propMng.GetRemainderCap(container);  //还可容纳的数量
                if (rCap < 1) //若不可容纳
                {
                    VWorld.SetLastError(ErrorCodes.ERROR_IMPLEMENTATION_LIMIT);
                    remainder?.Add(gItem);
                }
                else //若可以容纳
                {
                    ForcedMove(gItem, gItem.Count.Value, container, changes);
                }
            }
        }

        /// <summary>
        /// 移动一组物品到指定容器中。
        /// </summary>
        /// <param name="gItems"></param>
        /// <param name="container"></param>
        /// <param name="remainder"></param>
        /// <param name="changes"></param>
        public virtual void MoveItems(IEnumerable<GameItem> gItems, GameThingBase container, [AllowNull] ICollection<GameItem> remainder = null,
            [AllowNull] ICollection<GamePropertyChangeItem<object>> changes = null)
        {
            foreach (var gItem in gItems.ToArray()) //TODO 遍历每个物品
            {
                MoveItem(gItem, gItem.Count.Value, container, remainder, changes);
            }
        }

        /// <summary>
        /// 使用物品。非锁定函数，调用者自己负责同步。
        /// </summary>
        /// <param name="gItem"></param>
        /// <param name="count"></param>
        /// <param name="remainder"></param>
        /// <param name="changes"></param>
        public virtual bool UseItem(GameItem gItem, decimal count, [AllowNull] ICollection<GameItem> remainder = null, [AllowNull] ICollection<GamePropertyChangeItem<object>> changes = null)
        {
            bool result;
            var gc = gItem.GetGameChar();
            if (!gItem.TryGetProperty("usebpid", out var bpidObj) || !OwConvert.TryToGuid(bpidObj, out var bpid))
                bpid = Guid.Empty;
            if (bpid != Guid.Empty)    //若指定了蓝图
            {
                var bpMng = World.BlueprintManager;
                var template = bpMng.GetTemplateFromId(bpid) as BlueprintTemplate;
                using ApplyBlueprintDatas bpDatas = new ApplyBlueprintDatas(Service, gc)
                {
                    Count = (int)count,
                    Blueprint = template,
                };
                bpDatas.GameItems.Add(gItem);
                World.BlueprintManager.ApplyBluprint(bpDatas);
                if (bpDatas.HasError)
                {
                    VWorld.SetLastError(bpDatas.ErrorCode);
                    VWorld.SetLastErrorMessage(bpDatas.ErrorMessage);
                    result = false;
                }
                else
                {
                    OwHelper.SafeCopy(bpDatas.Remainder, remainder);
                    OwHelper.SafeCopy(bpDatas.PropertyChanges.Select(c => c.Clone() as GamePropertyChangeItem<object>), changes);
                    result = true;
                }
            }
            else //若未指定蓝图
            {
                for (int i = 0; i < count; i++)   //多次单个使用物品
                {
                    //获取要生成的物品
                    var gis = this.ToGameItems(gItem.Properties, "use");
                    //逐一加入物品
                    foreach (var item in gis)
                    {
                        var container = World.EventsManager.GetDefaultContainer(item, gc);
                        MoveItem(item, item.Count.Value, container, remainder, changes);
                    }
                    ForcedSetCount(gItem, gItem.Count.Value - 1, changes);
                }
                result = true;
            }
            return result;
        }

        #endregion 物品操作

        #region 项目特定代码

        /// <summary>
        /// 扫描坐骑是否变化，若变化了则加入相应坐骑图鉴。
        /// </summary>
        /// <param name="gameChar"></param>
        /// <param name="changes">变化数据容器。</param>
        public void ScanMountsIllustrated(GameChar gameChar, ICollection<GamePropertyChangeItem<object>> changes = null)
        {
            var bag = gameChar.GetZuojiBag();
            var gim = World.ItemManager;
            var mounts = bag.Children.Select(c => (Id: (gim.GetHeadTemplate(c).GId.Value, gim.GetBodyTemplate(c).GId.Value), Mounts: c));  //坐骑
            var ills = gim.GetOrCreateItem(gameChar, ProjectConstant.MountsIllSlotId).Children.Select(c => (Id: ((int)c.Properties.GetDecimalOrDefault("headtid"),
                (int)c.Properties.GetDecimalOrDefault("bodytid")), Ill: c));    //动物图鉴
            var addIds = mounts.Select(c => c.Id).Except(ills.Select(c => c.Id));
            var items = addIds.Join(mounts, c => c, c => c.Id, (l, r) => r); //尚无图鉴的坐骑

            foreach (var item in items.ToArray())
            {
                var tt = World.ItemTemplateManager.Id2Template.Values.FirstOrDefault(c => c.Properties.GetDecimalOrDefault("headtid") == item.Id.Item1 &&
                      c.Properties.GetDecimalOrDefault("bodytid") == item.Id.Item2);    //要添加的动物图鉴模板
                if (tt is null)
                    continue;
                var gi = new GameItem();
                World.EventsManager.GameItemCreated(gi, tt);
                var parent = World.EventsManager.GetDefaultContainer(gi, gameChar);
                World.ItemManager.MoveItem(gi, gi.Count ?? 1, parent, null, changes);
            }
        }

        #endregion 项目特定代码
    }

    public class AddItemsOrMailDatas : ChangeItemsAndMailWorkDatsBase
    {
        public AddItemsOrMailDatas([NotNull] IServiceProvider service, [NotNull] GameChar gameChar) : base(service, gameChar)
        {
        }

        public AddItemsOrMailDatas([NotNull] VWorld world, [NotNull] GameChar gameChar) : base(world, gameChar)
        {
        }

        public AddItemsOrMailDatas([NotNull] VWorld world, [NotNull] string token) : base(world, token)
        {
        }

        /// <summary>
        /// 要加入的物品和指定的容器的模板Id。
        /// </summary>
        public List<(GameItem, Guid)> Items { get; } = new List<(GameItem, Guid)>();
    }

    public class GetRankOfTuiguanDatas : ComplexWorkGameContext
    {
        public GetRankOfTuiguanDatas([NotNull] IServiceProvider service, [NotNull] GameChar gameChar) : base(service, gameChar)
        {
        }

        public GetRankOfTuiguanDatas([NotNull] VWorld world, [NotNull] GameChar gameChar) : base(world, gameChar)
        {
        }

        public GetRankOfTuiguanDatas([NotNull] VWorld world, [NotNull] string token) : base(world, token)
        {
        }


        public List<(Guid, decimal, string, decimal)> Prv { get; } = new List<(Guid, decimal, string, decimal)>();

        public List<(Guid, decimal, string, decimal)> Next { get; } = new List<(Guid, decimal, string, decimal)>();

        /// <summary>
        /// 自己的战力排名。
        /// </summary>
        public int Rank { get; set; }

        /// <summary>
        /// 自己的战力。
        /// </summary>
        public decimal Scope { get; set; }
    }

    public class UseItemsWorkDatas : ChangeItemsWorkDatasBase
    {
        public UseItemsWorkDatas([NotNull] IServiceProvider service, [NotNull] GameChar gameChar) : base(service, gameChar)
        {
        }

        public UseItemsWorkDatas([NotNull] VWorld world, [NotNull] GameChar gameChar) : base(world, gameChar)
        {
        }

        public UseItemsWorkDatas([NotNull] VWorld world, [NotNull] string token) : base(world, token)
        {
        }

        /// <summary>
        /// 要使用物品的唯一Id集合。
        /// </summary>
        public Guid ItemId { get; set; }

        /// <summary>
        /// 要使用的数量。
        /// </summary>
        public int Count { get; set; }

        private Tuple<GameItem, decimal> _Item;
        /// <summary>
        /// 要使用的物品。
        /// </summary>
        public Tuple<GameItem, decimal> Item
        {
            get
            {
                if (_Item is null)
                {
                    var gi = GameChar.AllChildren.FirstOrDefault(c => c.Id == ItemId);
                    if (gi is null)
                        throw new ArgumentException("有一个Id不是有效物品。");
                    _Item = Tuple.Create(gi, (decimal)Count);
                }
                return _Item;
            }
        }

        /// <summary>
        /// 实际成功使用的次数。
        /// </summary>
        public int SuccCount { get; set; }

        List<GameItem> _Remainder;

        /// <summary>
        /// 剩余的物品，由于约束无法放入的物品放在这个集合中。
        /// </summary>
        public List<GameItem> Remainder => _Remainder ??= new List<GameItem>();

    }

    /// <summary>
    /// 
    /// </summary>
    public class SellDatas : ChangeItemsWorkDatasBase
    {
        public SellDatas([NotNull] IServiceProvider service, [NotNull] GameChar gameChar) : base(service, gameChar)
        {
        }

        public SellDatas([NotNull] VWorld world, [NotNull] GameChar gameChar) : base(world, gameChar)
        {
        }

        public SellDatas([NotNull] VWorld world, [NotNull] string token) : base(world, token)
        {
        }

        public List<(Guid, decimal)> SellIds { get; } = new List<(Guid, decimal)>();
    }

    /// <summary>
    /// <see cref="GameItemManager.SetLineup(SetLineupDatas)"/>使用的参数和返回值封装类。
    /// </summary>
    public class SetLineupDatas : ChangeItemsWorkDatasBase
    {
        public SetLineupDatas([NotNull] IServiceProvider service, [NotNull] GameChar gameChar) : base(service, gameChar)
        {
        }

        public SetLineupDatas([NotNull] VWorld world, [NotNull] GameChar gameChar) : base(world, gameChar)
        {
        }

        public SetLineupDatas([NotNull] VWorld world, [NotNull] string token) : base(world, token)
        {
        }

        /// <summary>
        /// Item3是位置号，-1表示取消该坐骑在该阵容中的设置。
        /// </summary>
        public List<(Guid, int, decimal)> Settings { get; } = new List<(Guid, int, decimal)>();
    }

    public class ActiveStyleDatas : GameCharGameContext
    {
        public ActiveStyleDatas([NotNull] IServiceProvider service, [NotNull] GameChar gameChar) : base(service, gameChar)
        {
        }

        public ActiveStyleDatas([NotNull] VWorld world, [NotNull] GameChar gameChar) : base(world, gameChar)
        {
        }

        public ActiveStyleDatas([NotNull] VWorld world, [NotNull] string token) : base(world, token)
        {

        }

        /// <summary>
        /// 获取或设置激活的风格方案号。
        /// </summary>
        public int ActiveNumber { get; set; }

        public List<ChangeItem> ItemChanges { get; set; }

    }

    /// <summary>
    /// 封装<see cref="GameItemManager"/>扩展方法。
    /// </summary>
    public static class GameItemManagerExtensions
    {
        /// <summary>
        /// 用字典中的属性创建一组对象。
        /// </summary>
        /// <param name="manager"></param>
        /// <param name="bag"></param>
        /// <param name="prefix">键的前缀，省略或为null表示没有前缀。</param>
        /// <returns></returns>
        public static IEnumerable<GameItem> ToGameItems(this GameItemManager manager, IReadOnlyDictionary<string, object> bag, string prefix = null)
        {
            var props = bag.GetValuesWithoutPrefix(prefix);
            var dics = props.Select(c => c.ToDictionary(c2 => c2.Item1, c2 => c2.Item2));
            var eventMng = manager.World.EventsManager;
            List<GameItem> result = new List<GameItem>();
            foreach (var item in dics)
            {
                if (!item.ContainsKey("tid") && !item.ContainsKey("tt"))    //若没有模板数据
                    continue;
                if (!item.ContainsKey("tt") && item.GetGuidOrDefault("tid") == Guid.Empty)
                    continue;
                var gi = new GameItem();
                eventMng.GameItemCreated(gi, item);
                result.Add(gi);
            }
            return result;
        }

        public static byte[] ToByteArray([NotNull] this GameItemManager manager, [NotNull] IEnumerable<GameItem> gameItems)
        {
            MemoryStream ms = new MemoryStream();
            using (var sw = new BinaryWriter(ms))
            {
                manager.Fill(gameItems, sw);
                sw.Flush();
            }
            return ms.ToArray();
        }

        public static void Fill([NotNull] this GameItemManager manager, [NotNull] IEnumerable<GameItem> gameItems, [NotNull] BinaryWriter writer)
        {
            var count = gameItems.Count();
            writer.Write(count);
            foreach (var item in gameItems)
            {
                manager.Fill(item, writer);
            }
        }

        public static void Fill(this GameItemManager manager, GameItem gameItem, BinaryWriter writer)
        {
            writer.Write(OwConvert.ToString(gameItem.Properties) ?? string.Empty);  //这个属性需要最先被写入
            writer.Write(gameItem.GetClientString() ?? string.Empty);
            writer.Write(gameItem.Count);
            writer.Write(DateTime.UtcNow);   //TO DO 保留二进制兼容性
            writer.Write(gameItem.Id);
            writer.Write(gameItem.OwnerId);
            writer.Write(gameItem.ParentId);
            writer.Write(gameItem.TemplateId);
            writer.Write(gameItem.Children.Count);
            foreach (var item in gameItem.Children)
            {
                manager.Fill(item, writer);
            }
        }

        public static IEnumerable<GameItem> ToGameItems(this GameItemManager manager, BinaryReader reader)
        {
            var result = new List<GameItem>();
            manager.Fill(reader, result);
            return result;
        }

        public static IEnumerable<GameItem> ToGameItems(this GameItemManager manager, byte[] buffer)
        {
            if (buffer is null || buffer.Length <= 0)
                return Array.Empty<GameItem>();
            using var ms = new MemoryStream(buffer, false);
            using var reader = new BinaryReader(ms);
            return manager.ToGameItems(reader);
        }

        /// <summary>
        /// 填充对象，调用者需要调用<see cref="GameEventsManager.GameItemLoaded(GameItem)"/>以完成加载后的初始化工作。
        /// </summary>
        /// <param name="manager"></param>
        /// <param name="reader"></param>
        /// <param name="gameItem"></param>
        public static void Fill(this GameItemManager manager, BinaryReader reader, GameItem gameItem)
        {
            gameItem.PropertiesString = reader.ReadString();    //这个属性需要最先读取
            gameItem.SetClientString(reader.ReadString());
            gameItem.Count = reader.ReadNullableDecimal();
            _ = reader.ReadDateTime();  //TO DO 保留二进制兼容性
            gameItem.Id = reader.ReadGuid();
            gameItem.OwnerId = reader.ReadNullableGuid();
            gameItem.ParentId = reader.ReadNullableGuid();
            gameItem.TemplateId = reader.ReadGuid();
            var count = reader.ReadInt32();
            for (int i = 0; i < count; i++)
            {
                var gi = new GameItem();
                manager.Fill(reader, gi);
                gameItem.Children.Add(gi);
            }
            gameItem.Children.ForEach(c =>
            {
                c.Parent = gameItem;
            });
        }

        public static void Fill(this GameItemManager manager, BinaryReader reader, ICollection<GameItem> gameItems)
        {
            var count = reader.ReadInt32();
            for (int i = 0; i < count; i++)
            {
                var gi = new GameItem();
                manager.Fill(reader, gi);
                gameItems.Add(gi);
            }
        }

        /// <summary>
        /// 强制修改数量。
        /// </summary>
        /// <param name="mng"></param>
        /// <param name="gameItem"></param>
        /// <param name="diff"></param>
        /// <param name="changes"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool ForcedAddCount(this GameItemManager mng, GameItem gameItem, decimal diff, [AllowNull] ICollection<GamePropertyChangeItem<object>> changes = null) =>
            mng.ForcedSetCount(gameItem, gameItem.Count.GetValueOrDefault() + diff, changes);

    }

    /// <summary>
    /// 封装读取器和写入器的扩展方法。
    /// </summary>
    public static class BinaryExtensions
    {
        #region 读取器扩展

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Guid ReadGuid(this BinaryReader reader)
        {
            var guts = reader.ReadBytes(16);
            return new Guid(guts);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static DateTime ReadDateTime(this BinaryReader reader)
        {
            return new DateTime(reader.ReadInt64());
        }

        public static decimal? ReadNullableDecimal(this BinaryReader reader)
        {
            if (!reader.ReadBoolean())
                return null;
            return reader.ReadDecimal();
        }

        public static Guid? ReadNullableGuid(this BinaryReader reader)
        {
            if (!reader.ReadBoolean())
                return null;
            return reader.ReadGuid();
        }

        #endregion 读取器扩展

        #region 写入器扩展

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Write(this BinaryWriter writer, Guid guid)
        {
            writer.Write(guid.ToByteArray());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Write(this BinaryWriter writer, DateTime dateTime)
        {
            writer.Write(dateTime.Ticks);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Write(this BinaryWriter writer, decimal? obj)
        {
            writer.Write(obj.HasValue);
            if (obj.HasValue)
                writer.Write(obj.Value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Write(this BinaryWriter writer, Guid? obj)
        {
            writer.Write(obj.HasValue);
            if (obj.HasValue)
                writer.Write(obj.Value);
        }
        #endregion 写入器扩展
    }

    /// <summary>
    /// 带变化物品返回值的类的接口。
    /// </summary>
    public abstract class ChangeItemsWorkDatasBase : ComplexWorkGameContext
    {
        protected ChangeItemsWorkDatasBase([NotNull] IServiceProvider service, [NotNull] GameChar gameChar) : base(service, gameChar)
        {
        }

        protected ChangeItemsWorkDatasBase([NotNull] VWorld world, [NotNull] GameChar gameChar) : base(world, gameChar)
        {
        }

        protected ChangeItemsWorkDatasBase([NotNull] VWorld world, [NotNull] string token) : base(world, token)
        {
        }

        private List<ChangeItem> _ChangeItems;
        /// <summary>
        /// 工作后，物品变化数据。
        /// 不同操作自行定义该属性内的内容。
        /// </summary>
        public List<ChangeItem> ChangeItems { get => _ChangeItems ??= new List<ChangeItem>(); }

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
                _ChangeItems = null;
                base.Dispose(disposing);
            }
        }
    }

}
