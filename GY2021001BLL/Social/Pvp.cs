using GuangYuan.GY001.UserDb;
using Microsoft.EntityFrameworkCore;
using OW.Game;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace GuangYuan.GY001.BLL
{
    public class CharPvpDataView : GameCharWorkDataBase
    {
        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <param name="service"></param>
        /// <param name="gameChar"></param>
        public CharPvpDataView([NotNull] IServiceProvider service, [NotNull] GameChar gameChar) : base(service, gameChar)
        {
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <param name="world"></param>
        /// <param name="gameChar"></param>
        public CharPvpDataView([NotNull] VWorld world, [NotNull] GameChar gameChar) : base(world, gameChar)
        {
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <param name="world"></param>
        /// <param name="token"></param>
        public CharPvpDataView([NotNull] VWorld world, [NotNull] string token) : base(world, token)
        {
        }

        GameItem _GameItem;
        public GameItem GameItem
        {
            get
            {
                return _GameItem ??= GameChar.GetPvpObject();
            }
        }

        private List<Guid> _TodayIds;
        /// <summary>
        /// 今日刷到过的对手角色Id集合。
        /// </summary>
        public List<Guid> TodayIds
        {
            get
            {
                if (_TodayIds is null)
                {
                    var str = GameItem.Properties.GetStringOrDefault("TodayIds");
                    if (string.IsNullOrWhiteSpace(str))
                        _TodayIds = new List<Guid>();
                    else
                        _TodayIds = str.Split(Separator, StringSplitOptions.RemoveEmptyEntries).Select(c => Guid.Parse(c)).ToList();
                }
                return _TodayIds;
            }
        }

        private List<Guid> _LastIds;
        /// <summary>
        /// 最后一次刷新且可用的Id集合。每打过一个Id将删除。这个数据生成时就应合并到<see cref="TodayIds"/>中。
        /// </summary>
        public List<Guid> LastIds
        {
            get
            {
                if (_LastIds is null)
                {
                    var str = GameItem.Properties.GetStringOrDefault("LastIds");
                    if (string.IsNullOrWhiteSpace(str))
                        _LastIds = new List<Guid>();
                    else
                        _LastIds = str.Split(Separator, StringSplitOptions.RemoveEmptyEntries).Select(c => Guid.Parse(c)).ToList();
                }
                return _LastIds;
            }
        }

        /// <summary>
        /// 最后刷新的日期。
        /// </summary>
        public DateTime LastRefreshDate
        {
            get
            {
                return GameItem.Properties.GetDateTimeOrDefault("LastRefreshDate", DateTime.UtcNow.Date);
            }
            set
            {
                GameItem.Properties["LastRefreshDate"] = value.Date;
            }
        }

        /// <summary>
        /// "今日"概念上的时间。
        /// </summary>
        /// <value>默认值是构造对象的Utc时间。</value>
        public DateTime Today { get; set; } = DateTime.UtcNow;

        public void Save()
        {
            if (null != _LastIds)
                GameItem.Properties["LastIds"] = string.Join(Separator, _LastIds.Select(c => c.ToString()));
            if (null != _TodayIds)
                GameItem.Properties["TodayIds"] = string.Join(Separator, _TodayIds.Select(c => c.ToString()));
            bool dirty = false;
            if (null != _AllowAttackIds)
            {
                dirty = true;
            }

            if (dirty)
                UserContext.SaveChanges();
        }

        /// <summary>
        /// 新获取一组pvp目标角色Id集合。
        /// </summary>
        /// <returns></returns>
        public List<Guid> GetNewList()
        {
            //var result = from gi in UserContext.Set<GameItem>().Where(c => c.TemplateId == ProjectConstant.PvpObjectTId)
            //             join bag in UserContext.Set<GameItem>().Where(c => c.TemplateId == ProjectConstant.CurrencyBagTId && c.OwnerId.HasValue)
            //             on gi.ParentId equals bag.Id
            //             select gi;
            var bags = UserContext.Set<GameItem>().Where(c => c.TemplateId == ProjectConstant.CurrencyBagTId && c.OwnerId.HasValue);
            IEnumerable<Guid> excpColl;
            if (Today.Date == LastRefreshDate.Date)
                excpColl = TodayIds;
            else
                excpColl = Array.Empty<Guid>();
            var gis = UserContext.Set<GameItem>().Where(c => c.TemplateId == ProjectConstant.PvpObjectTId && c.Id != GameItem.Id);
            var collLow = from gi in gis
                          where gi.Count < GameItem.Count && !excpColl.Contains(bags.First(c => c.Id == gi.ParentId).OwnerId.Value)
                          orderby gi.Count descending
                          select gi;
            var collEqual = from gi in gis
                            where gi.Count == GameItem.Count && !excpColl.Contains(bags.First(c => c.Id == gi.ParentId).OwnerId.Value)
                            select gi;
            var collHigh = from gi in gis
                           where gi.Count > GameItem.Count && !excpColl.Contains(bags.First(c => c.Id == gi.ParentId).OwnerId.Value)
                           orderby gi.Count
                           select gi;
            var total = collEqual.Take(3).Concat(collLow.Take(3)).Concat(collHigh.Take(3)).Include(c => c.Parent).ToList();

            var tmpList = new List<GameItem>();
            var low = total.FirstOrDefault(c => c.Count < GameItem.Count);  //取一个较低的
            if (null != low)
                tmpList.Add(low);
            var equals = total.Where(c => c.Count == GameItem.Count).Take(2 - tmpList.Count);   //取相等的
            tmpList.AddRange(equals);
            var highs = total.Where(c => c.Count > GameItem.Count).Take(3 - tmpList.Count);
            tmpList.AddRange(highs);
            if (tmpList.Count < 3)
            {
                var _ = total.Where(c => !tmpList.Contains(c)).Take(3 - tmpList.Count);
                tmpList.AddRange(_);
            }
            return tmpList.Select(c => c.Parent.OwnerId.Value).ToList();
        }

        /// <summary>
        /// 刷新并记住该列表。
        /// </summary>
        public void RefreshList()
        {
            var ids = GetNewList();
            if (Today.Date != LastRefreshDate.Date)    //若刷新列表不是今天的
            {
                LastRefreshDate = Today;
                TodayIds.Clear();
            }
            LastIds.Clear();
            LastIds.AddRange(ids);
            TodayIds.AddRange(ids);
        }

        ObservableCollection<Guid> _AllowAttackIds;

        const int AllowPvpAttack = 10001;

        /// <summary>
        /// 此角色可以不耗费次数攻击的角色的Id集合。
        /// </summary>
        public ICollection<Guid> AllowAttackIds
        {
            get
            {
                if (_AllowAttackIds is null)
                {
                    var coll = from gsr in UserContext.Set<GameSocialRelationship>()
                               where gsr.Id == GameChar.Id && gsr.KeyType == AllowPvpAttack
                               select gsr.Id;
                    _AllowAttackIds = new ObservableCollection<Guid>(coll);
                    _AllowAttackIds.CollectionChanged += AllowAttackIdsCollectionChanged;
                }
                return _AllowAttackIds;
            }
        }

        private void AllowAttackIdsCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    UserContext.Set<GameSocialRelationship>().AddRange(
                        e.NewItems.OfType<Guid>().Select(c => new GameSocialRelationship(GameChar.Id, c, AllowPvpAttack, 0)));
                    break;
                case NotifyCollectionChangedAction.Remove:
                    break;
                case NotifyCollectionChangedAction.Replace:
                    break;
                case NotifyCollectionChangedAction.Reset:
                    UserContext.Set<GameSocialRelationship>().Local.Clear();
                    break;
                case NotifyCollectionChangedAction.Move:
                default:
                    break;
            }
        }
    }
}
