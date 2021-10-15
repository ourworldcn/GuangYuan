using GuangYuan.GY001.BLL;
using GuangYuan.GY001.UserDb;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OW.Game.Store;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
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
        /// <summary>
        /// 标志这是一个定时任务数据项。
        /// </summary>
        public const string SchedulerId = "34f7d9e7-358e-44cd-ab08-19575a046cb9";

        /// <summary>
        /// 记录参数的键名称。
        /// </summary>
        public const string ParamsKeyName = "ParamsKeyName";

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
