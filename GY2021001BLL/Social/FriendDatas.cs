using GuangYuan.GY001.UserDb;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OW.Game;
using Game.Social;
using Microsoft.EntityFrameworkCore;

namespace GuangYuan.GY001.BLL.Social
{
    /// <summary>
    /// 好友的工作数据。
    /// </summary>
    public class FriendDataView : IDisposable
    {
        public const string TotalRflKey = "totalRfl";
        public const string LastRflKey = "lastRfl";
        public const string RflDateTimeKey = "timeRfl";

        /// <summary>
        /// 分隔符。用于记录Id字符串数组的分隔符。
        /// </summary>
        public const char Separator = '/';

        public FriendDataView(VWorld world, GameChar gameChar, DateTime todayUtc)
        {
            World = world;
            GameChar = gameChar;
            _TodayUtc = todayUtc;
        }

        public VWorld World { get; }

        DateTime _TodayUtc;

        /// <summary>
        /// 校准的时间点。
        /// </summary>
        public DateTime TodayUtc { get => _TodayUtc; set => _TodayUtc = value; }

        public DateTime Today0 => TodayUtc.Date;

        public DateTime Tomorrow0 => Today0 + TimeSpan.FromDays(1);

        public GameChar GameChar { get; }

        private DbContext _Db;

        public DbContext DbCoutext
        {
            get { return _Db ??= World.CreateNewUserDbContext(); }
            set { _Db = value; }
        }

        /// <summary>
        /// 今日刷了数据否。
        /// </summary>
        public bool HasData
        {
            get
            {
                return GameChar.Properties.GetDateTimeOrDefault(RflDateTimeKey) == Today0;
            }
            set
            {
                if (value)
                {
                    GameChar.Properties[RflDateTimeKey] = Today0;
                }
                else
                {
                    GameChar.Properties.Remove(RflDateTimeKey);
                }
            }
        }

        List<Guid> _LastListIds;
        /// <summary>
        /// 最后一次刷新的可申请名单。
        /// </summary>
        public List<Guid> LastListIds
        {
            get
            {
                if (_LastListIds is null)
                {
                    var str = GameChar.Properties.GetStringOrDefault(LastRflKey);
                    if (HasData && !string.IsNullOrWhiteSpace(str))    //若有今天刷新数据
                    {
                        _LastListIds = GameChar.Properties.GetStringOrDefault(LastRflKey).Split(Separator, StringSplitOptions.RemoveEmptyEntries).Select(c => Guid.Parse(c)).ToList();
                    }
                    else
                    {
                        _LastListIds = new List<Guid>();
                    }
                }
                return _LastListIds;
            }
        }

        List<Guid> _TodayIds;
        /// <summary>
        /// 指定日刷新到的所有可能申请的好友Id集合。
        /// </summary>
        public List<Guid> TodayIds
        {
            get
            {
                if (_TodayIds is null)
                {
                    var str = GameChar.Properties.GetStringOrDefault(TotalRflKey);
                    if (HasData && !string.IsNullOrWhiteSpace(str))    //若有今天刷新数据
                    {
                        _TodayIds = str.Split(Separator, StringSplitOptions.RemoveEmptyEntries).Select(c => Guid.Parse(c)).ToList();
                    }
                    else
                    {
                        _TodayIds = new List<Guid>();
                    }
                }
                return _TodayIds;
            }
        }

        /// <summary>
        /// 获取新的可申请好友名单。
        /// </summary>
        /// <param name="bodyTIds">按展示坐骑身体模板Id过滤，如果是空则不过滤。</param>
        /// <returns></returns>
        public IQueryable<Guid> RefreshLastList(IEnumerable<Guid> bodyTIds)
        {
            IQueryable<Guid> result;
            var db = DbCoutext;
            IQueryable<GameSocialRelationship> shows;
            shows = db.Set<GameSocialRelationship>().Where(c => bodyTIds.Contains(c.Id2) && c.KeyType == SocialConstant.HomelandShowKeyType);  //展示坐骑
            //var allows = db.Set<CharSpecificExpandProperty>().Where(c => c.FrinedMaxCount > c.FrinedCount);   //有空位用户
            var todayList = TodayIds;   //今日已经刷过的用户
            var notAllows = World.SocialManager.GetFriendsOrRequestingOrBlackIds(GameChar.Id, db);  //好友或黑名单
            var tmpStr1 = $"{SocialConstant.ConfirmedFriendPName}=0";
            var frees = db.Set<GameSocialRelationship>().Where(c => c.PropertiesString.Contains(tmpStr1)).GroupBy(c => c.Id).Where(c => c.Count() >= 20).Select(c => c.Key); //未处理好友申请数量>20
            var slotTId = ProjectConstant.FriendSlotTId;
            if (bodyTIds.Any())
                result = from tmp in shows
                         where tmp.Id != GameChar.Id  //不能查出自己
                         where /*allows.Any(c => c.Id == tmp.Id) &&*/ !frees.Any(c => c == tmp.Id) && !todayList.Contains(tmp.Id) && !notAllows.Contains(tmp.Id)
                         group tmp by tmp.Id into g
                         //where g.Count()>0
                         orderby g.Count() descending
                         select g.Key;
            else
                result = from gi in db.Set<GameItem>()
                         where gi.TemplateId == slotTId && gi.Id != GameChar.Id  //不能查出自己
                         orderby gi.ExtraString descending
                         let gcId = gi.OwnerId ?? Guid.Empty
                         where /*allows.Any(c => c.Id == gcId) &&*/ !frees.Any(c => c == gcId) && !TodayIds.Contains(gcId) && !notAllows.Contains(gcId)
                         select gcId;
            return result;
        }

        //IQueryable cm = EF.CompileQuery((DbContext db, string name) => from tmp in db.Set<GameItem>().Where(c => c.TemplateId == ProjectConstant.FriendSlotTId)
        //                                                              orderby tmp.ExPropertyString descending
        //                                                              join gameChar in db.Set<GameChar>()
        //                                                              on tmp.OwnerId.Value equals gameChar.Id
        //                                                              where EF.Functions.Like(gameChar.DisplayName, $"%{name}%")
        //                                                              where /*allows.Any(c => c.Id == gameChar.Id) &&*/ !frees.Any(c => c == gameChar.Id) && !notAllows.Contains(gameChar.Id)
        //                                                              select tmp.OwnerId.Value);
        public IQueryable<Guid> RefreshLastList(string displayName)
        {
            var db = DbCoutext;
            //var allows = db.Set<CharSpecificExpandProperty>().Where(c => c.FrinedMaxCount > c.FrinedCount);   //有空位用户
            var notAllows = World.SocialManager.GetFriendsOrRequestingOrBlackIds(GameChar.Id, db);  //好友或黑名单
            var tmpStr1 = $"{SocialConstant.ConfirmedFriendPName}=0";
            var frees = db.Set<GameSocialRelationship>().Where(c => c.PropertiesString.Contains(tmpStr1)).GroupBy(c => c.Id).Where(c => c.Count() >= 20).Select(c => c.Key); //未处理好友申请数量>20
            var slotTId = ProjectConstant.FriendSlotTId;
            IQueryable<Guid> result;
            result = from tmp in db.Set<GameItem>().Where(c => c.TemplateId == slotTId)
                     orderby tmp.ExtraString descending
                     join gameChar in db.Set<GameChar>()
                     on tmp.OwnerId.Value equals gameChar.Id
                     where EF.Functions.Like(gameChar.DisplayName, $"%{displayName}%")
                     where /*allows.Any(c => c.Id == gameChar.Id) &&*/ !frees.Any(c => c == gameChar.Id) && !notAllows.Contains(gameChar.Id)
                     select tmp.OwnerId.Value;
            return result;
        }

        /// <summary>
        /// 保存数据。
        /// </summary>
        public void Save()
        {
            var db = GameChar.GameUser.DbContext;
            if (null != _LastListIds)
            {
                GameChar.Properties[LastRflKey] = string.Join(Separator, _LastListIds.Select(c => c.ToString()));
                World.CharManager.NotifyChange(GameChar.GameUser);
            }
            if (null != _TodayIds || null != _LastListIds) //若今日刷新过的列表不空或最后一次刷新的列表不空
            {
                _TodayIds = TodayIds.Union(LastListIds).ToList(); //合并列表
                GameChar.Properties[TotalRflKey] = string.Join(Separator, TodayIds.Select(c => c.ToString()));
                World.CharManager.NotifyChange(GameChar.GameUser);
            }
        }

        public void Dispose()
        {
            _Db?.Dispose();
            GC.SuppressFinalize(this);
        }
    }

}
