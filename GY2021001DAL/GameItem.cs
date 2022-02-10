using GuangYuan.GY001.TemplateDb;
using Microsoft.EntityFrameworkCore;
using OW.Game;
using OW.Game.Store;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace GuangYuan.GY001.UserDb
{
    /// <summary>
    /// 游戏内部事物的基类。
    /// </summary>
    [NotMapped]
    public abstract class GameItemBase : GameThingBase, IDisposable
    {
        #region 构造函数

        /// <summary>
        /// 构造函数。
        /// </summary>
        public GameItemBase()
        {

        }

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="id"><inheritdoc/></param>
        public GameItemBase(Guid id) : base(id)
        {

        }

        #endregion 构造函数
        /// <summary>
        /// 获取指定名称的属性名。调用<see cref="TryGetPropertyValue(string, out object)"/>来实现。
        /// </summary>
        /// <param name="propertyName"></param>
        /// <param name="defaultVal"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public object GetPropertyValueOrDefault(string propertyName, object defaultVal = default) =>
            TryGetPropertyValue(propertyName, out var result) ? result : defaultVal;

        /// <summary>
        /// 获取指定属性名称的属性值。
        /// </summary>
        /// <param name="propertyName">动态属性的名称。</param>
        /// <param name="result">动态属性的值。</param>
        /// <returns>true成功返回属性，false未找到属性。</returns>
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public virtual bool TryGetPropertyValue(string propertyName, out object result)
        {
            bool succ;
            switch (propertyName)
            {
                default:
                    if (Name2FastChangingProperty.TryGetValue(propertyName, out var fcp))   //若存在渐变属性
                    {
                        succ = true;
                        result = fcp.GetCurrentValueWithUtc();
                    }
                    else
                    {
                        succ = Properties.TryGetValue(propertyName, out result);
                        if (!succ && null != Template)
                            succ = Template.TryGetPropertyValue(propertyName, out result);
                    }
                    break;
            }
            return succ;
        }

        /// <summary>
        /// 设置一个属性。
        /// </summary>
        /// <param name="propertyName"></param>
        /// <param name="val"></param>
        /// <returns>true，如果属性名存在或确实应该有(基于某种需要)，且设置成功。false，设置成功一个不存在且不认识的属性。</returns>
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public virtual bool SetPropertyValue(string propertyName, object val)
        {
            bool succ;
            switch (propertyName)
            {
                default:
                    succ = TryGetPropertyValue(propertyName, out var oldVal);
                    if (!succ || !Equals(oldVal, val))
                    {
                        Properties[propertyName] = val;
                        succ = true;
                    }
                    break;
            }
            return succ;
        }

        #region 快速变化属性相关

        /// <summary>
        /// 获取属性，且考虑是否刷新并写入快速变化属性。
        /// </summary>
        /// <param name="name">要获取值的属性名。</param>
        /// <param name="refreshDate">当有快速变化属性时，刷新时间，如果为null则不刷新。</param>
        /// <param name="writeDictionary">当有快速变化属性时，是否写入<see cref="Properties"/>属性。</param>
        /// <param name="result">属性的当前返回值。对快速变化属性是其<see cref="FastChangingProperty.LastValue"/>,是否在之前刷新取决于<paramref name="refresh"/>参数。</param>
        /// <param name="refreshDatetime">如果是快速变化属性且需要刷新，则此处返回实际的计算时间。
        /// 如果找到的不是快速渐变属性返回<see cref="DateTime.MinValue"/></param>
        /// <returns>true成功找到属性。</returns>
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public virtual bool TryGetPropertyValueWithFcp(string name, DateTime? refreshDate, bool writeDictionary, out object result, out DateTime refreshDatetime)
        {
            bool succ;
            if (Name2FastChangingProperty.TryGetValue(name, out var fcp)) //若找到快速变化属性
            {
                if (refreshDate.HasValue) //若需要刷新
                {
                    refreshDatetime = refreshDate.Value;
                    result = fcp.GetCurrentValue(ref refreshDatetime);
                }
                else
                {
                    refreshDatetime = DateTime.MinValue;
                    result = fcp.LastValue;
                }
                if (writeDictionary)
                    fcp.ToGameThing(this);
                succ = true;
            }
            else //若是其他属性
            {
                refreshDatetime = DateTime.MinValue;
                succ = Properties.TryGetValue(name, out result);
            }
            return succ;
        }

        /// <summary>
        ///  获取属性，若是快速变化属性时会自动用当前时间刷新且写入<see cref="Properties"/>。
        /// </summary>
        /// <param name="name"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGetPropertyValueWithFcp(string name, out object result)
        {
            DateTime dt = DateTime.UtcNow;
            return TryGetPropertyValueWithFcp(name, dt, true, out result, out _);
        }

        #endregion 快速变化属性相关

        #region 扩展属性相关

        #endregion 扩展属性相关

        #region 事件及相关
        /// <summary>
        /// 通知该实例，即将保存到数据库。
        /// </summary>
        /// <param name="e"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override void PrepareSaving(DbContext db)
        {
            base.PrepareSaving(db);
        }

        #endregion 事件及相关

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
                }

                // TODO: 释放未托管的资源(未托管的对象)并重写终结器
                // TODO: 将大型字段设置为 null
                base.Dispose(disposing);
            }
        }
    }

    /// <summary>
    /// 游戏中物品，装备，货币，积分的基类。
    /// </summary>
    [Table("GameItems")]
    public class GameItem : GameItemBase, IDisposable
    {
        /// <summary>
        /// 构造函数。
        /// </summary>
        public GameItem()
        {

        }

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="id">指定Id。</param>
        public GameItem(Guid id) : base(id)
        {

        }

        [JsonIgnore]
        [NotMapped]
        public override DbContext DbContext => GameChar?.DbContext;

        private decimal? _Count;

        /// <summary>
        /// 此物品的数量。
        /// 可能没有数量属性，如装备永远是1。对货币类(积分)都使用的是实际值。
        /// </summary>
        public decimal? Count
        {
            get
            {
                if (_Count.HasValue)
                    return _Count.Value;
                if (Name2FastChangingProperty is null)
                    return _Count;
                return Name2FastChangingProperty.TryGetValue("Count", out var fcp) ? fcp.LastValue : _Count;
            }

            set
            {
                if (Name2FastChangingProperty.TryGetValue("Count", out var fcp) && value.HasValue)
                    fcp.LastValue = value.Value;
                _Count = value;
            }
        }

        /// <summary>
        /// 如果物品处于某个容器中，则这个成员指示其所处位置号，从0开始，但未必连续,序号相同则顺序随机。
        /// </summary>
        [NotMapped] //TO DO
        public int OrderNumber { get; set; }

        /// <summary>
        /// 所属槽导航属性。
        /// </summary>
        [JsonIgnore]
        public virtual GameItem Parent { get; set; }

        /// <summary>
        /// 所属槽Id。
        /// </summary>
        [ForeignKey(nameof(Parent))]
        public Guid? ParentId { get; set; }

        /// <summary>
        /// 拥有的子物品或槽。
        /// </summary>
        public virtual List<GameItem> Children { get; set; } = new List<GameItem>();

        /// <summary>
        /// 获取该物品直接或间接下属对象的枚举数。深度优先。
        /// </summary>
        /// <returns>枚举数。不包含自己。枚举过程中不能更改树节点的关系。</returns>
        [NotMapped]
        [JsonIgnore]
        public IEnumerable<GameItem> AllChildren
        {
            get
            {
                foreach (var item in Children)
                {
                    yield return item;
                    foreach (var item2 in item.AllChildren)
                    {
                        yield return item2;
                    }
                }
            }
        }

        /// <summary>
        /// 所属角色Id或其他关联对象的Id。
        /// </summary>
        public Guid? OwnerId { get; set; }

        private GameChar _GameChar;
        /// <summary>
        /// 获取或设置所属的角色对象。没有设置关系可能返回null。
        /// </summary>
        [NotMapped]
        [JsonIgnore]
        public GameChar GameChar
        {
            get
            {
                return _GameChar ?? Parent?.GameChar;
            }
            set => _GameChar = value;
        }


        /// <summary>
        /// 容器的Id。可能返回容器Id。
        /// </summary>
        [NotMapped]
        [JsonIgnore]
        public Guid? ContainerId => (ParentId ?? Parent?.Id) ?? OwnerId;

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            var result = (Template as GameItemTemplate)?.DisplayName ?? (Template as GameItemTemplate)?.Remark;
            if (string.IsNullOrWhiteSpace(result))
            {
                return base.ToString();
            }

            return $"{{{result},{Count}}}";
        }

        /// <summary>
        /// 木材Id，这个不是槽，它的Count属性直接记录了数量，目前其子代为空。
        /// </summary>
        public static readonly Guid MucaiId = new Guid("{01959584-E2C9-4E54-BBB7-FCC58A9484EC}");

        /// <summary>
        /// 木材仓库模板Id。
        /// </summary>
        public static readonly Guid MucaiStoreTId = new Guid("{8caea73b-e210-47bf-a121-06cc12973baf}");

        /// <summary>
        /// 堆叠上限属性的名字。没有该属性的不可堆叠，无上限限制用-1表示。
        /// </summary>
        public const string StackUpperLimit = "stc";

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <param name="propertyName"><inheritdoc/></param>
        /// <param name="result"><inheritdoc/></param>
        /// <returns><inheritdoc/></returns>
        public override bool TryGetPropertyValue(string propertyName, out object result)
        {
            bool succ;
            switch (propertyName)
            {
                case StackUpperLimit when TemplateId == MucaiId: //对木材特殊处理 TO DO应控制反转完成该工作
                    var coll = Parent?.AllChildren ?? GameChar?.GameItems;
                    if (coll is null)
                    {
                        result = 0m;
                        return false;
                    }
                    var ary = coll.Where(c => c.TemplateId == MucaiStoreTId).ToArray();   //取所有木材仓库对象
                    if (!OwConvert.TryToDecimal(Properties.GetValueOrDefault(StackUpperLimit, 0m), out var myselfStc))
                        myselfStc = 0;
                    result = ary.Any(c => c.GetStc() >= decimal.MaxValue) ? -1 : ary.Sum(c => c.GetStc()) + myselfStc;
                    succ = true;
                    break;
                case "count":
                case "Count":
                    var obj = Count;
                    succ = obj.HasValue;
                    result = obj ?? 0;
                    break;
                default:
                    succ = base.TryGetPropertyValue(propertyName, out result);
                    break;
            }
            return succ;
        }

        /// <summary>
        /// 试图转换为<see cref="GameItemTemplate"/>,如果不能转化则返回null。
        /// </summary>
        [NotMapped]
        [JsonIgnore]
        public GameItemTemplate ItemTemplate
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Template as GameItemTemplate;
        }

        /// <summary>
        /// 获取指定属性的当前级别索引值。
        /// </summary>
        /// <param name="name"></param>
        /// <returns>如果不是序列属性或索引属性值不是数值类型则返回-1。如果没有找到索引属性返回0。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetIndexPropertyValue(string name)
        {
            if (!Template.TryGetPropertyValue(name, out var tVal) || !(tVal is decimal[]))  //若不是序列属性
                return -1;
            var indexPName = Template.GetIndexPropertyName(name);   //其索引属性名
            int result;
            if (!TryGetPropertyValue(indexPName, out var resultObj))    //若没有找到索引属性的值
                result = 0;
            else //若找到索引属性
                result = OwConvert.TryToDecimal(resultObj, out var resultDec) ? OwHelper.RoundWithAwayFromZero(resultDec) : -1;
            return result;
        }

        /// <summary>
        /// 换新模板。
        /// </summary>
        /// <param name="template"></param>
        public void ChangeTemplate(GameItemTemplate template)
        {
            var keysBoth = Properties.Keys.Intersect(template.Properties.Keys).ToArray();
            var keysNew = template.Properties.Keys.Except(keysBoth).ToArray();
            foreach (var key in keysNew)    //新属性
            {
                var newValue = template.GetPropertyValue(key);
                if (newValue is decimal[] ary)   //若是一个序列属性
                {
                    var indexName = template.GetIndexPropertyName(key); //索引属性名
                    if ((TryGetPropertyValue(indexName, out var indexObj) || template.TryGetPropertyValue(indexName, out indexObj)) &&
                        OwConvert.TryToDecimal(indexObj, out var index))
                    {
                        index = Math.Round(index, MidpointRounding.AwayFromZero);
                        SetPropertyValue(key, ary[(int)index]);
                    }
                    else
                        SetPropertyValue(key, ary[0]);
                }
                else
                    SetPropertyValue(key, newValue);
            }
            foreach (var key in keysBoth)   //遍历两者皆有的属性
            {
                var currentVal = GetPropertyValueOrDefault(key);
                var oldVal = Template.GetPropertyValue(key);    //模板值
                if (oldVal is decimal[] ary && OwConvert.TryToDecimal(currentVal, out var currentDec))   //若是一个序列属性
                {
                    var lv = GetIndexPropertyValue(key);    //当前等级
                    var nVal = currentDec - ary[lv] + template.GetSequencePropertyValueOrDefault<decimal>(key, lv); //求新值
                    SetPropertyValue(key, nVal);
                }
                else if (OwConvert.TryToDecimal(currentVal, out var dec)) //若是一个数值属性
                {
                    OwConvert.TryToDecimal(Template.GetPropertyValue(key, 0), out var nDec);    //当前模板中该属性
                    OwConvert.TryToDecimal(template.GetPropertyValue(key), out var tDec);
                    var nVal = dec - nDec + tDec;
                    SetPropertyValue(key, nVal);
                }
                else //其他类型属性
                {
                    SetPropertyValue(key, template.GetPropertyValue(key));
                }
            }
            TemplateId = template.Id;
            Template = template;
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
                    Children.ForEach(c => c.Dispose());
                }

                // TODO: 释放未托管的资源(未托管的对象)并重写终结器
                // TODO: 将大型字段设置为 null
                Parent = null;
                _GameChar = null;
                base.Dispose(disposing);
            }
        }

        // // TODO: 仅当“Dispose(bool disposing)”拥有用于释放未托管资源的代码时才替代终结器
        // ~GameItemBase()
        // {
        //     // 不要更改此代码。请将清理代码放入“Dispose(bool disposing)”方法中
        //     Dispose(disposing: false);
        // }

        #endregion IDisposable接口相关

    }

    public static class GameItemExtensions
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="name"></param>
        /// <param name="newValue"></param>
        /// <param name="tag"></param>
        /// <param name="changes">变化数据的集合，如果值变化了，将向此集合追加变化数据对象。若省略或为null则不追加。</param>
        /// <returns>true设置了变化数据，false,新值与旧值没有变化。</returns>
        public static bool SetPropertyAndReturnChangedItem(this GameItem obj, string name, object newValue, [AllowNull] object tag = null,
            [AllowNull] ICollection<GamePropertyChangedItem<object>> changes = null)
        {
            if (0 == string.Compare(name, "count")) //若是Count属性
            {
                if (newValue is null)    //若新值是null
                {
                    if (obj.Count is null)   //若原值也是空
                        return false;
                    else if (null != changes)  //若需要变化数据
                    {
                        var item = GamePropertyChangedItemPool<object>.Shared.Get();
                        item.Object = obj; item.PropertyName = name; item.Tag = tag;
                        item.HasOldValue = true;
                        item.OldValue = obj.Count.Value;
                        changes.Add(item);
                    }
                    obj.Count = null;
                    return true;
                }
                else    //若新值不是null
                {
                    if (!OwConvert.TryToDecimal(newValue, out var newCount))    //若无法转换为decimal
                        throw new ArgumentException("必须能转换为decimal类型", nameof(newValue));
                    if (obj.Count is null || obj.Count.Value != newCount)  //若有变化
                    {
                        if (null != changes)  //若需要变化数据
                        {
                            var item = GamePropertyChangedItemPool<object>.Shared.Get();
                            item.Object = obj; item.PropertyName = name; item.Tag = tag;
                            if (!(obj.Count is null))
                            {
                                item.HasOldValue = true;
                                item.OldValue = obj.Count.Value;
                            }
                            item.HasNewValue = true;
                            item.NewValue = newValue;
                            changes.Add(item);
                        }
                        obj.Count = newCount;
                        return true;
                    }
                    else //若无变化
                    {
                        return false;
                    }
                }
            }
            else
                return SimpleDynamicPropertyBaseExtensions.SetPropertyAndReturnChangedItem(obj, name, newValue, tag, changes);
        }

        public static void ToE(IEnumerable<GamePropertyChangedItem<object>> src, ICollection<ChangeItem> dest)
        {
            var coll = from tmp in src
                       group tmp by tmp.Object;
            coll.Select(c => c.Key);
        }

        /// <summary>
        /// 获取堆叠空余数量。
        /// </summary>
        /// <param name="obj"></param>
        /// <returns>堆叠空余数量，不可堆叠将返回0，不限制将返回<see cref="decimal.MaxValue"/></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static decimal GetNumberOfStackRemainder(this GameItem obj)
        {
            if (!obj.IsStc(out decimal stc))
                return 0;
            return -1 == stc ? decimal.MaxValue : Math.Max(0, stc - obj.Count.Value);
        }

    }

    /// <summary>
    /// 记录虚拟物品、资源变化的类。
    /// </summary>
    [NotMapped]
    public class ChangeItem
    {
        /// <summary>
        /// 按容器Id化简集合。
        /// </summary>
        /// <param name="changes"></param>
        public static void Reduce(List<ChangeItem> changes)
        {
            if (changes is null)
            {
                throw new ArgumentNullException(nameof(changes));
            }
            //合并容器
            var _ = from tmp in changes
                    group tmp by tmp.ContainerId into g
                    where g.Count() > 1
                    select (g.Key, g.First(), g.Skip(1).ToArray());
            var lst = _.ToList();
            foreach (var item in lst)
            {
                item.Item2.Adds.AddRange(item.Item3.SelectMany(c => c.Adds));
                item.Item2.Changes.AddRange(item.Item3.SelectMany(c => c.Changes));
                item.Item2.Removes.AddRange(item.Item3.SelectMany(c => c.Removes));
            }
            foreach (var item in lst.SelectMany(c => c.Item3))
            {
                changes.Remove(item);
            }
            //对容器内数据合并
            var tmpList = new List<GameItem>();
            var ids = new List<Guid>();
            foreach (var item in changes)
            {
                //合并追加数据
                tmpList.Clear();
                tmpList.AddRange(item.Adds.Distinct());
                if (tmpList.Count != item.Adds.Count)
                {
                    item.Adds.Clear();
                    item.Adds.AddRange(tmpList);
                }
                //合并删除数据
                ids.Clear();
                ids.AddRange(item.Removes.Distinct());
                if (ids.Count != item.Removes.Count)
                {
                    item.Removes.Clear();
                    item.Removes.AddRange(ids);
                }
                //合并改变数据
                tmpList.Clear();
                tmpList.AddRange(item.Changes.Distinct());
                if (tmpList.Count != item.Changes.Count)
                {
                    item.Changes.Clear();
                    item.Changes.AddRange(tmpList);
                }
            }
            for (int i = changes.Count - 1; i >= 0; i--)    //去除空的项
            {
                var item = changes[i];
                if (item.IsEmpty)
                {
                    changes.RemoveAt(i);
                }
            }
        }

        /// <summary>
        /// 构造函数。
        /// </summary>
        public ChangeItem()
        {

        }

        public ChangeItem(Guid containerId, IEnumerable<GameItem> adds = null, IEnumerable<Guid> removes = null, IEnumerable<GameItem> changes = null)
        {
            ContainerId = containerId;
            if (null != adds)
            {
                Adds.AddRange(adds);
            }

            if (null != removes)
            {
                Removes.AddRange(removes);
            }

            if (null != changes)
            {
                Changes.AddRange(changes);
            }
        }

        /// <summary>
        /// 容器的Id。如果是容器本身属性变化，这个成员是容器的上层容器Id,例如背包的容量变化了则这个成员就是角色Id。
        /// </summary>
        public DateTime DateTimeUtc { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// 容器的实例Id:<see cref="GuidKeyBase.Id"/>。
        /// </summary>
        public Guid ContainerId { get; set; }

        private List<GameItem> _Adds;
        /// <summary>
        /// 增加的数据。
        /// </summary>
        public List<GameItem> Adds => _Adds ??= new List<GameItem>();

        private List<Guid> _Removes;

        /// <summary>
        /// 删除的对象的唯一Id集合。
        /// </summary>
        public List<Guid> Removes => _Removes ??= new List<Guid>();

        private List<GameItem> _Changes;
        /// <summary>
        /// 变化的数据。
        /// </summary>
        public List<GameItem> Changes => _Changes ??= new List<GameItem>();

        /// <summary>
        /// 获取一个指示，这个对象内变化数据是空的。
        /// </summary>
        public bool IsEmpty => (_Adds?.Count ?? 0) + (_Removes?.Count ?? 0) + (_Changes?.Count ?? 0) == 0;
    }

    /// <summary>
    /// <see cref="ChangesItem"/>相关的扩展方法。
    /// </summary>
    public static class ChangesItemExtensions
    {
        public static void ToE(IEnumerable<GamePropertyChangedItem<object>> src, ICollection<ChangeItem> dest)
        {
            var coll = from tmp in src
                       group tmp by tmp.Object;
            coll.Select(c => c.Key);
        }

        /// <summary>
        /// 追加物品到追加数据中。
        /// </summary>
        /// <param name="coll"></param>
        /// <param name="containerId"></param>
        /// <param name="items">即使没有指定参数，也会增加容器。</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AddToAdds(this ICollection<ChangeItem> coll, Guid containerId, params GameItem[] items)
        {
            var item = coll.FirstOrDefault(c => c.ContainerId == containerId);
            if (null == item)
            {
                item = new ChangeItem() { ContainerId = containerId };
                coll.Add(item);
            }
            for (int i = 0; i < items.Length; i++)
            {
                item.Adds.Add(items[i]);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AddToAdds(this ICollection<ChangeItem> coll, params GameItem[] items)
        {
            foreach (var item in items)
                coll.AddToAdds(item.ContainerId.Value, item);
        }

        /// <summary>
        /// 追加物品到移除数据中。
        /// </summary>
        /// <param name="coll"></param>
        /// <param name="containerId"></param>
        /// <param name="items"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AddToRemoves(this ICollection<ChangeItem> coll, Guid containerId, params Guid[] items)
        {
            var item = coll.FirstOrDefault(c => c.ContainerId == containerId);
            if (null == item)
            {
                item = new ChangeItem() { ContainerId = containerId };
                coll.Add(item);
            }
            for (int i = 0; i < items.Length; i++)
            {
                item.Removes.Add(items[i]);
            }
        }

        /// <summary>
        /// 追加物品到变化数据中。
        /// </summary>
        /// <param name="coll"></param>
        /// <param name="containerId"></param>
        /// <param name="items"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AddToChanges(this ICollection<ChangeItem> coll, Guid containerId, params GameItem[] items)
        {
            var item = coll.FirstOrDefault(c => c.ContainerId == containerId);
            if (null == item)
            {
                item = new ChangeItem() { ContainerId = containerId };
                coll.Add(item);
            }
            for (int i = 0; i < items.Length; i++)
            {
                item.Changes.Add(items[i]);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AddToChanges(this ICollection<ChangeItem> coll, params GameItem[] items)
        {
            //TO DO以后优化
            //var tuples = from item in items
            //             join change in coll
            //             on item.ContainerId.Value equals change.ContainerId into g
            //             from tmp in g.DefaultIfEmpty() //左链
            //             select (tmp, item);
            //foreach (var item in tuples)
            //{
            //    item.change.Changes.Add(item.item);
            //}
            foreach (var item in items)
                coll.AddToChanges(item.ContainerId.Value, item);
        }
    }

}
