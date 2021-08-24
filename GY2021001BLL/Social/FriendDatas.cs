using GuangYuan.GY001.UserDb;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OW.Game;

namespace GuangYuan.GY001.BLL.Social
{
    /// <summary>
    /// 好友的工作数据。
    /// </summary>
    public class FriendDatas
    {
        public FriendDatas(VWorld world, GameChar gameChar, DateTime todayUtc)
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
        public DateTime TodayUtc => _TodayUtc;

        public DateTime Today0 => TodayUtc.Date;

        public DateTime Tomorrow0 => Today0 + TimeSpan.FromDays(1);

        public GameChar GameChar { get; }

        const string TotalRflKey = "totalRfl";
        const string LastRflKey = "lastRfl";
        const string RflDateTimeKey = "timeRfl";

        /// <summary>
        /// 今日刷了数据否。
        /// </summary>
        public bool HasData => GameChar.Properties.GetDateTimeOrDefault(RflDateTimeKey) == Today0;

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
                    if (HasData)    //若有今天刷新数据
                    {
                        _LastListIds = GameChar.Properties.GetStringOrDefault(LastRflKey).Split(';', StringSplitOptions.RemoveEmptyEntries).Select(c => Guid.Parse(c)).ToList();
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

                }
                return _TodayIds;
            }
        }

        /// <summary>
        /// 刷新最后一次的名单。
        /// </summary>
        public void RefreshLastList()
        {

        }
    }

}
