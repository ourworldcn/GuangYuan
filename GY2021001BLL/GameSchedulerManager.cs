using GuangYuan.GY001.UserDb;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OW.Game;
using OW.Game.Store;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace GuangYuan.GY001.BLL
{
    public class SchedulerDescriptor : GuidKeyObjectBase, IDisposable
    {
        public const string ClassId = "261f6fa1-5845-4974-a7f3-ffb974364b76";

        public static implicit operator GameActionRecord(SchedulerDescriptor obj)
        {
            var result = new GameActionRecord()
            {
                Id = obj.Id,
                ActionId = ClassId,
                Remark = "任务计划项。",
            };
            result.Properties[nameof(Properties)] = Uri.EscapeDataString(OwHelper.ToPropertiesString(obj.Properties)); //记录参数
            result.Properties[nameof(ComplatedDatetime)] = obj.ComplatedDatetime.ToString("s"); //记录定时的时间
            result.Properties[nameof(ServiceTypeName)] = obj.ServiceTypeName; //记录方法名
            result.Properties[nameof(MethodName)] = obj.MethodName; //记录方法名
            return result;
        }

        public static explicit operator SchedulerDescriptor(GameActionRecord obj)
        {
            var result = new SchedulerDescriptor()
            {
                Id = obj.Id,
                ComplatedDatetime = obj.Properties.GetDateTimeOrDefault(nameof(ComplatedDatetime)),
                ServiceTypeName = obj.Properties.GetStringOrDefault(nameof(ServiceTypeName)),   //服务名
                MethodName = obj.Properties.GetStringOrDefault(nameof(MethodName)), //记录方法名

            };
            OwHelper.AnalysePropertiesString(Uri.UnescapeDataString(obj.Properties.GetStringOrDefault(nameof(Properties))), result.Properties);
            return result;
        }

        /// <summary>
        /// 构造函数。
        /// </summary>
        public SchedulerDescriptor()
        {
        }

        public SchedulerDescriptor(Guid id) : base(id)
        {
        }

        /// <summary>
        /// 完成的时间点。
        /// </summary>
        public DateTime ComplatedDatetime { get; set; }

        /// <summary>
        /// 服务的类型的名字，通常是类型全名。
        /// </summary>
        public string ServiceTypeName { get; set; }

        /// <summary>
        /// 获取或设置方法名。
        /// 不设置或为null则不调用特定方法。
        /// </summary>
        public string MethodName { get; set; }

        Dictionary<string, object> _Properties;
        /// <summary>
        /// 额外参数的字典，只能放置可转换为字符串的类型。
        /// </summary>
        public Dictionary<string, object> Properties =>
            _Properties ??= new Dictionary<string, object>();

        /// <summary>
        /// 记录使用的计时器。
        /// </summary>
        internal Timer Timer { get; set; }

        private bool disposedValue;

        /// <summary>
        /// 是否已经处置。
        /// </summary>
        public bool IsDisposed => disposedValue;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: 释放托管状态(托管对象)
                    Timer?.Dispose();
                }

                // TODO: 释放未托管的资源(未托管的对象)并重写终结器
                // TODO: 将大型字段设置为 null
                _Properties = null;
                Timer = null;
                MethodName = null;
                disposedValue = true;
            }
        }

        // // TODO: 仅当“Dispose(bool disposing)”拥有用于释放未托管资源的代码时才替代终结器
        // ~SchedulerData()
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

    }

    public class SchedulerManagerOptions
    {

    }

    /// <summary>
    /// 任务计划管理器。
    /// </summary>
    [DisplayName("任务计划管理器")]
    public class GameSchedulerManager : GameManagerBase<SchedulerManagerOptions>
    {
        public GameSchedulerManager()
        {
            Initialize();
        }

        public GameSchedulerManager(IServiceProvider service) : base(service)
        {
            Initialize();
        }

        public GameSchedulerManager(IServiceProvider service, SchedulerManagerOptions options) : base(service, options)
        {
            Initialize();
        }

        private void Initialize()
        {
            using var db = World.CreateNewUserDbContext();
            _Id2Descriptor = new ConcurrentDictionary<Guid, SchedulerDescriptor>(
                db.ActionRecords.AsNoTracking().Where(c => c.ActionId == SchedulerDescriptor.ClassId).ToDictionary(c => c.Id, c =>
                {
                    var result = (SchedulerDescriptor)c;
                    result.Timer = new Timer(TimerCallbackHandel);
                    return result;
                }));
            foreach (var item in _Id2Descriptor)
            {
                var ts = OwHelper.ComputeTimeout(DateTime.UtcNow, item.Value.ComplatedDatetime);
                item.Value.Timer.Change(ts, Timeout.InfiniteTimeSpan);
            }
            var logger = Service.GetService<ILogger<GameSchedulerManager>>();
            logger.LogInformation("任务计划管理器开始工作。");
        }

        /// <summary>
        /// 所有计划任务。
        /// </summary>
        ConcurrentDictionary<Guid, SchedulerDescriptor> _Id2Descriptor = new ConcurrentDictionary<Guid, SchedulerDescriptor>();

        /// <summary>
        /// 计时器到期使用的处理函数。
        /// </summary>
        /// <param name="state"></param>
        private void TimerCallbackHandel(object state)
        {
            if (state is SchedulerDescriptor sd)
            {
                try
                {
                    if (!sd.IsDisposed)
                        OnScheduler(sd);
                }
                finally
                {
                    if (_Id2Descriptor.TryRemove(sd.Id, out var tmp))
                    {
                        var sql = $"DELETE FROM [ActionRecords] WHERE [id] = '{tmp.Id}' AND [ActionId]='{SchedulerDescriptor.ClassId}'";
                        World.AddToUserContext(sql);
                        using var disposer = tmp;
                    }
                }
            }
        }

        /// <summary>
        /// 安排一个定时任务。
        /// </summary>
        /// <param name="state">任务的参数。</param>
        /// <param name="complatedTime">完成时间。</param>
        /// <returns>任务的Id。</returns>
        public virtual void Scheduler(SchedulerDescriptor data)
        {
            //记录信息
            var gar = (GameActionRecord)data;
            World.AddToUserContext(new object[] { gar });
            _Id2Descriptor[data.Id] = data;
            var ts = OwHelper.ComputeTimeout(DateTime.UtcNow, data.ComplatedDatetime);
            data.Timer = new Timer(TimerCallbackHandel, gar, ts, Timeout.InfiniteTimeSpan);

        }

        public bool Change(SchedulerDescriptor data)
        {
            if (!_Id2Descriptor.TryGetValue(data.Id, out var sd))
                return false;
            {
                if (data.IsDisposed) //若已经执行
                    return false;
                sd.Dispose();
                Scheduler(data);
            }
            return true;
        }

        /// <summary>
        /// 延迟任务到期时调用。派生类可以重载此函数。
        /// </summary>
        /// <param name="state"></param>
        /// <param name="id"></param>
        protected virtual void OnScheduler(SchedulerDescriptor data)
        {
            var type = Type.GetType(data.ServiceTypeName);
            var service = Service.GetService(type);
            var method = type.GetMethod(data.MethodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            switch (data.MethodName)
            {
                case nameof(Upgraded): //延迟升级结束
                    Upgraded(data);
                    break;
                default:
                    break;
            }
        }

        private void Upgraded(SchedulerDescriptor data)
        {
            var dic = data.Properties;
            var charId = dic.GetGuidOrDefault("charId");    //角色Id
            var itemId = dic.GetGuidOrDefault("itemId");    //物品Id
            using var dwUser = World.CharManager.LockOrLoad(charId, out var gu);
            if (dwUser is null)
            {
                //TO DO
                return;
            }
            GameChar gc = null;
            if (charId != Guid.Empty)
                gc = gu.GameChars.FirstOrDefault(c => c.Id == charId);
            var gi = gc?.AllChildren?.FirstOrDefault(c => c.Id == itemId);
            if (gi is null)
            {
                //TO DO
                return;
            }
            var fcp = gi.RemoveFastChangingProperty(ProjectConstant.LevelPropertyName);
            if (null != fcp)  //若存在升级冷却
            {
                var lv = gi.Properties.GetDecimalOrDefault(ProjectConstant.LevelPropertyName);  //当前级别
                World.ItemManager.SetPropertyValue(gi, ProjectConstant.LevelPropertyName, lv + 1);
                gi.Properties.Remove("UpgradedSchedulerId");    //删除定时任务Id
            }
        }


    }
}
