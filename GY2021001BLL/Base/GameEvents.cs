using GuangYuan.GY001.BLL;
using GuangYuan.GY001.UserDb;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace OW.Game
{
    public class GameEventsManagerOptions
    {

    }

    /// <summary>
    /// 事件服务。
    /// </summary>
    public class GameEventsManager : GameManagerBase<GameEventsManagerOptions>
    {
        public const string SchedulerId = "34f7d9e7-358e-44cd-ab08-19575a046cb9";
        public const string ComplatedTimeKeyName = "b44d2fa7-ad56-498d-8777-bb4e92a6de54";
        public const string UpgradeId = "0a24b846-d523-4271-8766-bcf68d729d08";

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
            using var db = World.CreateNewUserDbContext();
            _SchedulerDic = new ConcurrentDictionary<Guid, (GameActionRecord, Timer)>(
                db.ActionRecords.AsNoTracking().Where(c => c.ActionId == SchedulerId).ToDictionary(c => c.Id, c =>
                 {
                     var dt = c.Properties.GetDateTimeOrDefault(ComplatedTimeKeyName);
                     var ts = OwHelper.ComputeTimeout(DateTime.UtcNow, dt);
                     var tm = new Timer(TimerCallbackHandel, c, ts, Timeout.InfiniteTimeSpan);
                     return (c, tm);
                 }));
        }

        private ConcurrentDictionary<Guid, (GameActionRecord, Timer)> _SchedulerDic;

        /// <summary>
        /// 安排一个定时任务。
        /// </summary>
        /// <param name="state">任务的参数。</param>
        /// <param name="complatedTime">完成时间。</param>
        /// <returns>任务的Id。</returns>
        public virtual Guid Scheduler(IDictionary<string, object> state, DateTime complatedTime)
        {
            Trace.Assert(!state.ContainsKey(ComplatedTimeKeyName)); //诚心捣乱的抛异常
            //记录信息
            var gar = new GameActionRecord()
            {
                ActionId = SchedulerId,
                Remark = "定时任务项。",
            };
            foreach (var item in state)
                gar.Properties[item.Key] = item.Value;
            gar.Properties[ComplatedTimeKeyName] = complatedTime.ToString("s"); //记录定时的时间
            var ts = OwHelper.ComputeTimeout(DateTime.UtcNow, complatedTime);
            var tm = new Timer(TimerCallbackHandel, gar, ts, Timeout.InfiniteTimeSpan);
            _SchedulerDic[gar.Id] = (gar, tm);
            World.AddToUserContext(new object[] { gar });

            return gar.Id;
        }

        /// <summary>
        /// 安排一个定时任务。
        /// </summary>
        /// <param name="charId"></param>
        /// <param name="itemId"></param>
        /// <param name="state"></param>
        /// <param name="complatedTime"></param>
        /// <returns></returns>
        public virtual Guid Scheduler(Guid charId, Guid itemId, string state, DateTime complatedTime)
        {
            var dic = World.StringObjectDictionaryPool.Get();
            try
            {
                dic["charId"] = charId;
                dic["itemId"] = itemId;
                dic["state"] = state;
                return Scheduler(dic, complatedTime);
            }
            finally
            {
                World.StringObjectDictionaryPool.Return(dic);
            }
        }

        /// <summary>
        /// 计时器到期使用的处理函数。
        /// </summary>
        /// <param name="state"></param>
        private void TimerCallbackHandel(object state)
        {
            if (state is GameActionRecord gar && gar.ActionId == SchedulerId)
            {
                try
                {
                    OnScheduler(gar.Properties, gar.Id);
                }
                finally
                {
                    if (_SchedulerDic.TryRemove(gar.Id, out var tmp))
                    {
                        var sql = $"DELETE FROM [ActionRecords] WHERE [id] = '{gar.Id}' AND [ActionId]='{SchedulerId}'";
                        World.AddToUserContext(sql);
                        using var disposer = tmp.Item2;
                    }
                }
            }
        }

        /// <summary>
        /// 延迟任务到期时调用。派生类可以重载此函数。此实现立即返回。
        /// </summary>
        /// <param name="state"></param>
        /// <param name="id"></param>
        public virtual void OnScheduler(IDictionary<string, object> state, Guid id)
        {
        }

        public virtual void OnDynamicPropertyChanged(GameThingBase gameThing, IEnumerable<(string, object)> nameAndOldValue)
        {
            var lvName = World.PropertyManager.LevelPropertyName;
            foreach (var item in nameAndOldValue)
            {
                if (item.Item1 == lvName)
                {
                    OnLvChanged(gameThing, (int)Convert.ToDecimal(item.Item2));
                    break;
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
    }

    public class Gy001GameEventsManagerOptions : GameEventsManagerOptions
    {

    }

    public class Gy001GameEventsManager : GameEventsManager
    {

        public Gy001GameEventsManager()
        {
        }

        public Gy001GameEventsManager(IServiceProvider service) : base(service)
        {
        }

        public Gy001GameEventsManager(IServiceProvider service, GameEventsManagerOptions options) : base(service, options)
        {
        }

        public override void OnScheduler(IDictionary<string, object> state, Guid id)
        {
            var dic = state as IReadOnlyDictionary<string, object>;
            var charId = dic.GetGuidOrDefault("charId");    //角色Id
            var itemId = dic.GetGuidOrDefault("itemId");    //物品Id
            var str = dic.GetStringOrDefault("state");  //参数

            using var dwUser = World.CharManager.LockOrLoad(charId, out var gu);
            if (dwUser is null)
            {
                //TO DO
                return;
            }
            GameChar gc = null;
            if (charId != Guid.Empty)
                gc = gu.GameChars.FirstOrDefault(c => c.Id == charId);
            switch (str)
            {
                case UpgradeId: //若时升级延时结束
                    {
                        var gi = gc?.AllChildren?.FirstOrDefault(c => c.Id == itemId);
                        if (gi is null)
                        {//TO DO
                            return;
                        }
                        var lv = gi.Properties.GetDecimalOrDefault(ProjectConstant.LevelPropertyName);  //当前级别
                        World.ItemManager.SetPropertyValue(gi, ProjectConstant.LevelPropertyName, lv + 1);
                    }
                    break;
                default:
                    break;
            }
            base.OnScheduler(state, id);
        }

        public override void OnDynamicPropertyChanged(GameThingBase gameThing, IEnumerable<(string, object)> nameAndOldValue)
        {
            base.OnDynamicPropertyChanged(gameThing, nameAndOldValue);
            if (gameThing is GameItem gi && gi.TemplateId == ProjectConstant.MainControlRoomSlotId) //主控室变化
            {
                foreach (var item in nameAndOldValue)
                {
                    if (item.Item1 == ProjectConstant.LevelPropertyName)    //若主控室升级了
                    {
                        var newLv = gi.Properties.GetDecimalOrDefault(ProjectConstant.LevelPropertyName);
                        var oldLv = Convert.ToDecimal(item.Item2);
                        if (newLv == oldLv + 1)
                        {

                        }
                        break;
                    }
                }
            }
        }

        public override void OnGameItemAdd(IEnumerable<GameItem> gameItems, Dictionary<string, object> parameters)
        {
            base.OnGameItemAdd(gameItems, parameters);
        }
    }

}
