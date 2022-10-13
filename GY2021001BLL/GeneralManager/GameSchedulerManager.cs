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
            result.SetSdp(nameof(Properties), Uri.EscapeDataString(OwConvert.ToString(obj.Properties))); //记录参数
            result.SetSdp(nameof(ComplatedDatetime), obj.ComplatedDatetime.ToString("s")); //记录定时的时间
            result.SetSdp(nameof(ServiceTypeName), Uri.EscapeDataString(obj.ServiceTypeName)); //记录方法名
            result.SetSdp(nameof(MethodName), obj.MethodName); //记录方法名
            return result;
        }

        public static explicit operator SchedulerDescriptor(GameActionRecord obj)
        {
            var result = new SchedulerDescriptor()
            {
                Id = obj.Id,
                ComplatedDatetime = obj.GetSdpDateTimeOrDefault(nameof(ComplatedDatetime)),
                ServiceTypeName = Uri.UnescapeDataString(obj.GetSdpStringOrDefault(nameof(ServiceTypeName))),   //服务名
                MethodName = obj.GetSdpStringOrDefault(nameof(MethodName)), //记录方法名
            };
            OwConvert.Copy(Uri.UnescapeDataString(obj.GetSdpStringOrDefault(nameof(Properties))), result.Properties);
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

        private Dictionary<string, object> _Properties;
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
                    // 释放托管状态(托管对象)
                    Timer?.Dispose();
                }

                // 释放未托管的资源(未托管的对象)并重写终结器
                // 将大型字段设置为 null
                _Properties = null;
                Timer = null;
                MethodName = null;
                disposedValue = true;
            }
        }

        // 仅当“Dispose(bool disposing)”拥有用于释放未托管资源的代码时才替代终结器
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
        public SchedulerManagerOptions()
        {

        }
    }

    /// <summary>
    /// 任务计划管理器。
    /// 安排持久化任务，在用户注销或服务器重启后任务也可以得到执行。
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
                    result.Timer = new Timer(TimerCallbackHandel, result, Timeout.Infinite, Timeout.Infinite);
                    return result;
                }));
            foreach (var item in _Id2Descriptor)
            {
                lock (item.Value)
                {
                    if (item.Value.IsDisposed)
                        continue;
                    var ts = OwHelper.ComputeTimeout(DateTime.UtcNow, item.Value.ComplatedDatetime);
                    item.Value.Timer.Change(ts, Timeout.InfiniteTimeSpan);
                }
            }
            var logger = Service.GetService<ILogger<GameSchedulerManager>>();
            logger.LogInformation("任务计划管理器开始工作。");
        }

        /// <summary>
        /// 所有计划任务。
        /// </summary>
        private ConcurrentDictionary<Guid, SchedulerDescriptor> _Id2Descriptor = new ConcurrentDictionary<Guid, SchedulerDescriptor>();

        /// <summary>
        /// 计时器到期使用的处理函数。
        /// </summary>
        /// <param name="state"></param>
        private void TimerCallbackHandel(object state)
        {
            var sd = (SchedulerDescriptor)state;
            lock (sd)
            {
                if (!sd.IsDisposed) //若该工作项有效
                {
                    var id = sd.Id;
                    try
                    {
                        OnScheduler(sd);
                    }
                    finally
                    {
                        sd.Dispose();
                        _Id2Descriptor.Remove(id, out _);
                        var sql = $"DELETE FROM [ActionRecords] WHERE [Id] = '{id}' AND [ActionId]='{SchedulerDescriptor.ClassId}'";
                        World.AddToUserContext(sql);
                    }
                }
                else //若已经因为并发问题被清理
                {
                    //TO DO
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
            data.Timer = new Timer(TimerCallbackHandel, data, ts, Timeout.InfiniteTimeSpan);

        }

        /// <summary>
        /// 移除一个计划项。
        /// </summary>
        /// <param name="id"></param>
        /// <returns>true成功移除，false没有找到或已经由其他线程移除了。</returns>
        public bool Remove(Guid id)
        {
            if (!_Id2Descriptor.TryGetValue(id, out var sd))
                return false;
            lock (sd)
            {
                if (sd.IsDisposed)
                    return false;
                using (sd)
                    _Id2Descriptor.Remove(sd.Id, out _);
                return true;
            }
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
            var method = type.GetMethod(data.MethodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.FlattenHierarchy, null, new Type[] { typeof(SchedulerDescriptor) }, null);
            method?.Invoke(service, new object[] { data });
        }

        //private void Upgraded(SchedulerDescriptor data)
        //{
        //    var dic = data.Properties;
        //    var charId = dic.GetGuidOrDefault("charId");    //角色Id
        //    var itemId = dic.GetGuidOrDefault("itemId");    //物品Id
        //    using var dwUser = World.CharManager.LockOrLoad(charId, out var gu);
        //    if (dwUser is null)
        //    {
        //        //TO DO
        //        return;
        //    }
        //    GameChar gc = null;
        //    if (charId != Guid.Empty)
        //        gc = gu.GameChars.FirstOrDefault(c => c.Id == charId);
        //    var gi = gc?.AllChildren?.FirstOrDefault(c => c.Id == itemId);
        //    if (gi is null)
        //    {
        //        //TO DO
        //        return;
        //    }
        //    var fcp = gi.RemoveFastChangingProperty(World.PropertyManager.LevelPropertyName);
        //    if (null != fcp)  //若存在升级冷却
        //    {
        //        var lv = gi.GetSdpDecimalOrDefault(World.PropertyManager.LevelPropertyName);  //当前级别
        //        World.ItemManager.SetPropertyValue(gi, World.PropertyManager.LevelPropertyName, lv + 1);
        //        gi.Properties.Remove("UpgradedSchedulerId");    //删除定时任务Id
        //    }
        //}


    }
}
