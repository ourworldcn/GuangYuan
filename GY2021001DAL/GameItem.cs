using GuangYuan.GY001.TemplateDb;
using Microsoft.EntityFrameworkCore;
using OW.Game.PropertyChange;
using OW.Game.Store;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
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
                    succ = this.TryGetProperty(propertyName, out var oldVal);
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
                    // 释放托管状态(托管对象)
                }

                // 释放未托管的资源(未托管的对象)并重写终结器
                // 将大型字段设置为 null
                base.Dispose(disposing);
            }
        }
    }

    /// <summary>
    /// 游戏中物品，装备，货币，积分的基类。
    /// </summary>
    [Table("GameItems")]
    public class GameItem : GameItemBase, IVirtualThing<GameItem>, IDisposable, IValidatableObject
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

        /// <summary>
        /// 获取相关的数据库上下文，若没有导航属性则返回null。
        /// </summary>
        /// <returns></returns>
        public override DbContext GetDbContext()
        {
            return this.GetGameChar()?.GetDbContext();
        }

        /// <summary>
        /// 此物品的数量。
        /// 可能没有数量属性，如装备永远是1。对货币类(积分)都使用的是实际值。
        /// </summary>
        [NotMapped, JsonIgnore]
        public decimal? Count
        {
            get
            {
                if (null != Name2FastChangingProperty && Name2FastChangingProperty.TryGetValue(nameof(Count), out var fcp))
                    return fcp.LastValue;
                if (Properties.TryGetDecimal(nameof(Count), out var result))
                    return result;
                return null;
            }

            set
            {
                if (null != Name2FastChangingProperty && Name2FastChangingProperty.TryGetValue(nameof(Count), out var fcp) && value.HasValue)
                    fcp.LastValue = value.Value;
                Properties[nameof(Count)] = value;
            }
        }

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
        /// 所属角色Id或其他关联对象的Id。
        /// </summary>
        public Guid? OwnerId { get; set; }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            var result = this.GetTemplate()?.DisplayName ?? this.GetTemplate()?.Remark;
            if (string.IsNullOrWhiteSpace(result))
            {
                return base.ToString();
            }

            return $"{{{result},{Count}}}";
        }

        #region ISimpleDynamicExtensionProperty相关

        public override void SetSdep(string name, object value)
        {
            switch (name)
            {
                case "Count":
                case "count":
                    Count = Convert.ToDecimal(value);
                    break;
                default:
                    base.SetSdep(name, value);
                    break;
            }
        }

        public override bool TryGetSdep(string name, out object value)
        {
            switch (name)
            {
                case "Count":
                case "count":
                    value = Count;
                    return true;
                default:
                    return base.TryGetSdep(name, out value);
            }
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <returns><inheritdoc/></returns>
        public override IEnumerable<(string, object)> GetAllSdep()
        {
            return base.GetAllSdep().Concat(new (string, object)[] { (nameof(Count), Count) });
        }

        #endregion ISimpleDynamicExtensionProperty相关

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
                    Children.ForEach(c => c.Dispose());
                }

                // 释放未托管的资源(未托管的对象)并重写终结器
                // 将大型字段设置为 null
                Parent = null;
                base.Dispose(disposing);
            }
        }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            return Array.Empty<ValidationResult>();
        }

        // 仅当“Dispose(bool disposing)”拥有用于释放未托管资源的代码时才替代终结器
        // ~GameItemBase()
        // {
        //     // 不要更改此代码。请将清理代码放入“Dispose(bool disposing)”方法中
        //     Dispose(disposing: false);
        // }

        #endregion IDisposable接口相关

    }

    /// <summary>
    /// 封装一些与<see cref="GameItem"/>相关的扩展方法。
    /// </summary>
    public static class GameItemExtensions
    {
        /// <summary>
        /// 获取模板对象。
        /// </summary>
        /// <param name="gItem"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static GameItemTemplate GetTemplate(this GameItem gItem) =>
            ((GameThingBase)gItem).GetTemplate() as GameItemTemplate;

        /// <summary>
        /// 设置模板对象。
        /// </summary>
        /// <param name="thing"></param>
        /// <param name="template"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetTemplate(this GameItem gItem, GameItemTemplate template) =>
            ((GameThingBase)gItem).SetTemplate(template);

        public static IEnumerable<GameItem> GetAllChildren(this GameItem gameItem)
        {
            foreach (var item in gameItem.Children)
            {
                yield return item;
                foreach (var item2 in item.GetAllChildren())
                {
                    yield return item2;
                }
            }
        }

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
            [AllowNull] ICollection<GamePropertyChangeItem<object>> changes = null)
        {
            if (0 == string.Compare(name, "count")) //若是Count属性
            {
                if (newValue is null)    //若新值是null
                {
                    if (obj.Count is null)   //若原值也是空
                        return false;
                    else if (null != changes)  //若需要变化数据
                    {
                        var item = GamePropertyChangeItemPool<object>.Shared.Get();
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
                            var item = GamePropertyChangeItemPool<object>.Shared.Get();
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
                return SimpleDynamicPropertyBaseExtensions.SetPropertyAndAddChangedItem(obj, name, newValue, tag, changes);
        }

        /// <summary>
        /// 获取或设置所属的角色对象。没有设置关系可能返回null。
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static GameChar GetGameChar(this GameItem obj)
        {
            return obj.RuntimeProperties.GetValueOrDefault("GameChar", null) as GameChar ?? obj.Parent?.GetGameChar();
        }

        /// <summary>
        /// 获取或设置所属的角色对象。没有设置关系可能返回null。
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetGameChar(this GameItem obj, GameChar value)
        {
            if (value is null)
                obj.RuntimeProperties.Remove("GameChar", out _);
            else
                obj.RuntimeProperties["GameChar"] = value;
        }

        /// <summary>
        /// 容器的Id。可能返回容器Id。
        /// </summary>
        public static Guid? GetContainerId(this GameItem gameItem)
        {
            return (gameItem.ParentId ?? gameItem.Parent?.Id) ?? gameItem.OwnerId;
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
                coll.AddToAdds(item.GetContainerId().Value, item);
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
                coll.AddToChanges(item.GetContainerId().Value, item);
        }
    }

}
