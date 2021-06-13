using Gy2021001Template;
using OwGame;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace GY2021001DAL
{
    /// <summary>
    /// 游戏中物品，装备，货币，积分的基类。
    /// </summary>
    [DebuggerDisplay("{" + nameof(GetDebuggerDisplay) + "(),nq}")]
    public class GameItem : GameThingBase
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
        /// 此物品的数量。
        /// 可能没有数量属性，如装备永远是1。对货币类(积分)都使用的是实际值。
        /// </summary>
        public decimal? Count { get; set; }

        /// <summary>
        /// 如果物品处于某个容器中，则这个成员指示其所处位置号，从0开始，但未必连续。
        /// </summary>
        [NotMapped] //TO DO
        public int? OrderNumber { get; set; }

        /// <summary>
        /// 所属槽导航属性。
        /// </summary>
        public virtual GameItem Parent { get; set; }

        /// <summary>
        /// 所属槽Id。
        /// </summary>
        [ForeignKey(nameof(Parent))]
        public Guid? ParentId { get; set; }

        /// <summary>
        /// 拥有的子物品或槽。
        /// </summary>
        public virtual List<GameItem> Children { get; } = new List<GameItem>();

        /// <summary>
        /// 所属角色Id或其他关联对象的Id。
        /// </summary>
        public Guid? OwnerId { get; set; }

        protected string GetDebuggerDisplay()
        {
            if (Properties.TryGetValue("tname", out object obj) && obj is string result)
                return $"TName = {result}";
            return ToString();
        }

        public override string ToString()
        {
            var result = (Template as GameItemTemplate)?.DisplayName ?? (Template as GameItemTemplate)?.Remark;
            if (null == result)
                return base.ToString();
            return $"{{{result},{Count}}}";
        }
    }

    /// <summary>
    /// 记录虚拟物品、资源变化的类。
    /// </summary>
    [NotMapped]
    public class ChangesItem
    {
        /// <summary>
        /// 按容器Id化简集合。
        /// </summary>
        /// <param name="changes"></param>
        public static void Reduce(List<ChangesItem> changes)
        {
            if (changes is null)
                throw new ArgumentNullException(nameof(changes));
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
                    changes.RemoveAt(i);
            }
        }

        public ChangesItem()
        {

        }

        public ChangesItem(Guid containerId, IEnumerable<GameItem> adds = null, IEnumerable<Guid> removes = null, IEnumerable<GameItem> changes = null)
        {
            ContainerId = containerId;
            if (null != adds)
                Adds.AddRange(adds);
            if (null != removes)
                Removes.AddRange(removes);
            if (null != changes)
                Changes.AddRange(changes);
        }

        /// <summary>
        /// 容器的Id。如果是容器本身属性变化，这个成员是容器的上层容器Id,例如背包的容量变化了则这个成员就是角色Id。
        /// </summary>
        public DateTime DateTimeUtc { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// 容器的实例Id:<see cref="GuidKeyBase.Id"/>。
        /// </summary>
        public Guid ContainerId { get; set; }

        List<GameItem> _Adds;
        /// <summary>
        /// 增加的数据。
        /// </summary>
        public List<GameItem> Adds => _Adds ??= new List<GameItem>();

        List<Guid> _Removes;

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

}
