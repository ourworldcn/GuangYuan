using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using OW.Game.Store;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace OW.Game
{
    public class DataObjectManagerOptions : IOptions<DataObjectManagerOptions>
    {
        public DataObjectManagerOptions Value => this;

        /// <summary>
        /// 锁定键的默认超时。
        /// </summary>
        /// <value>默认值:3秒钟。</value>
        public TimeSpan DefaultLockTimeout { get; set; } = TimeSpan.FromSeconds(3);

        /// <summary>
        /// 扫描间隔。
        /// </summary>
        /// <value>默认值:1分钟。</value>
        public TimeSpan ScanFrequency { get; internal set; } = TimeSpan.FromMinutes(1);

        /// <summary>
        /// 默认的缓存超时。
        /// </summary>
        /// <value>默认值:1分钟。</value>
        public TimeSpan DefaultCachingTimeout { get; set; } = TimeSpan.FromMinutes(1);
    }

    /// <summary>
    /// 数据对象设置。
    /// </summary>
    public class DataObjectOptions
    {
        public DataObjectOptions()
        {
        }

        public string Key { get; set; }

        /// <summary>
        /// 加载时调用。
        /// 在对键加锁的范围内调用。
        /// </summary>
        [AllowNull]
        public Func<string, object, object> LoadCallback { get; set; }

        /// <summary>
        /// <see cref="LoadCallback"/>的用户参数。
        /// </summary>
        [AllowNull]
        public object LoadCallbackState { get; set; }

        /// <summary>
        /// 需要保存时调用。
        /// 在对键加锁的范围内调用。
        /// 回调参数是要保存的对象，附加数据，返回true表示成功，否则是没有保存成功,若没有设置该回调，则说民无需保存，也就视同保存成功。
        /// </summary>
        [AllowNull]
        public Func<object, object, bool> SaveCallback { get; set; }

        /// <summary>
        /// <see cref="SaveCallback"/>的用户参数。
        /// </summary>
        [AllowNull]
        public object SaveCallbackState { get; set; }

        /// <summary>
        /// 从缓存中移除后调用。
        /// 在对键加锁的范围内。
        /// </summary>
        public Action<object, object> LeaveCallback { get; set; }

        /// <summary>
        /// <see cref="LeaveCallback"/>的用户参数。
        /// </summary>
        [AllowNull]
        public object LeaveCallbackState { get; set; }

        /// <summary>
        /// 从对象获取字符串类型键的函数，默认<see cref="GuidKeyObjectBase.IdString"/>。如果不是该类型或派生对象，请设置这个成员。
        /// </summary>
        public Func<object, string> GetKeyCallback { get; set; } = c => ((GuidKeyObjectBase)c).IdString;

        public object Value
        {
            get
            {
                return default;
            }
        }
    }

    /// <summary>
    /// 数据对象的管理器，负责单例加载，保存并自动驱逐。
    /// </summary>
    public class DataObjectManager
    {
        #region 构造函数

        public DataObjectManager()
        {
            Options = new DataObjectManagerOptions();
            Initialize();
        }

        public DataObjectManager(IOptions<DataObjectManagerOptions> options)
        {
            Options = options.Value;
            Initialize();
        }

        /// <summary>
        /// 初始化。
        /// </summary>
        void Initialize()
        {
            _Datas = new DataObjectCache(new DataObjectCacheOptions()
            {
                LockCallback = (obj, timeout) => StringLocker.TryEnter((string)obj, timeout),
                UnlockCallback = c => StringLocker.Exit((string)c),
                DefaultLockTimeout = _Options.DefaultLockTimeout,
            });
            _Timer = new Timer(TimerCallback, null, Options.ScanFrequency, Options.ScanFrequency);
        }

        #endregion 构造函数

        public void TimerCallback(object state)
        {
            _Datas.Compact();
            Save();
        }

        Timer _Timer;

        DataObjectManagerOptions _Options;

        public DataObjectManagerOptions Options { get => _Options; set => _Options = value; }

        MemoryCacheBase _Datas;

        HashSet<string> _Dirty = new HashSet<string>();

        /// <summary>
        /// 对标记为脏的数据进行保存。
        /// </summary>
        protected void Save()
        {
            List<string> keys = new List<string>();
            lock (_Dirty)
            {
                OwHelper.Copy(_Dirty, keys);
                _Dirty.Clear();
            }
            for (int i = keys.Count - 1; i >= 0; i--)
            {
                var key = keys[i];
                using (var dw = DisposeHelper.Create(StringLocker.TryEnter, StringLocker.Exit, key, TimeSpan.Zero))
                {
                    if (dw.IsEmpty)
                        continue;
                    var entry = _Datas.GetCacheEntry(key);
                    if (entry is null)  //若键下的数据已经销毁
                    {
                        keys.RemoveAt(i);
                        continue;
                    }
                    try
                    {
                        var option = (DataObjectOptions)entry.State;
                        if (null != option.SaveCallback && !(bool)option.SaveCallback?.Invoke(entry.Value, option.SaveCallbackState))
                            continue;
                        keys.RemoveAt(i);
                    }
                    catch (Exception)
                    {
                    }
                }
            }
            //放入下次再保存
            if (keys.Count > 0)
                lock (_Dirty)
                    OwHelper.Copy(keys, _Dirty);
        }

        /// <summary>
        /// 获取或加载对象。
        /// </summary>
        /// <param name="key"></param>
        /// <param name="loader"></param>
        /// <returns></returns>
        public object GetOrLoad(string key, Action<DataObjectOptions> loader)
        {
            key = StringLocker.Intern(key);
            using var dw = DisposeHelper.Create(StringLocker.TryEnter, StringLocker.Exit, key, Options.DefaultLockTimeout);
            if (dw.IsEmpty)
                return null;
            var entry = _Datas.GetCacheEntry(key);
            DataObjectOptions options;
            if (entry is null)
            {
                using var entity = _Datas.CreateLeafCacheEntry(key);
                options = new DataObjectOptions()
                {
                    Key = key,
                };
                entity.State = options;
                loader(options);
                entity.SetSlidingExpiration(_Options.DefaultCachingTimeout);
                entity.RegisterPostEvictionCallback((object key, object value, EvictionReason reason, object state) =>
                {
                    options.LeaveCallback?.Invoke(value, state);
                });
                if (options.LoadCallback != null)
                    entity.Value = options.LoadCallback(key, options.LoadCallbackState);
            }
            entry = _Datas.GetCacheEntry(key);
            Debug.Assert(null != entry);
            return entry.Value;
        }

        /// <summary>
        /// 在本地中获取对象，如果没有则返回null。
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public object Find(string key)
        {
            using var dw = DisposeHelper.Create(StringLocker.TryEnter, StringLocker.Exit, key, Options.DefaultLockTimeout);
            if (dw.IsEmpty)
                return null;
            return _Datas.TryGetValue(key, out var result) ? result : default;
        }

        /// <summary>
        /// 指出对象已经更改，需要保存。
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public bool SetDirty(string key)
        {
            lock (_Dirty)
                return _Dirty.Add(key);
        }

        #region 后台工作相关

        public Task StartAsync(CancellationToken cancellationToken)
        {
            //var result = base.StartAsync(cancellationToken);
            return Task.CompletedTask;
        }

        protected Task ExecuteAsync(CancellationToken stoppingToken)
        {
            return Task.CompletedTask;
        }

        #endregion 后台工作相关

        //public bool SetTimout(string key, TimeSpan timeout)
        //{
        //    using var dw = DisposeHelper.Create(StringLocker.TryEnter, StringLocker.Exit, key, Options.Timeout);
        //    if (dw.IsEmpty)
        //    {
        //        return false;
        //    }
        //    if (!_Datas.TryGetValue(key, out var entity))
        //    {
        //        return false;
        //    }
        //    entity.Timeout = timeout;
        //    return true;
        //}

    }

    public static class DataObjectManagerExtensions
    {
        public static DataObjectManager GetOrLoad<T>(this DataObjectManager mng, string key, Func<DbContext> dbCreator)
        {
            mng.GetOrLoad(key, options =>
            {
                var db = dbCreator();
                options.SaveCallbackState = db;
                options.SaveCallback = (obj, state) => { ((DbContext)state).SaveChanges(); return true; };
                options.LeaveCallbackState = db;

            });
            return mng;
        }
    }
}
