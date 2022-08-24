using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace Microsoft.Extensions.Caching.Memory
{
    public class EfObjectCacheOptions : DataObjectCacheOptions, IOptions<EfObjectCacheOptions>
    {
        public EfObjectCacheOptions() : base()
        {
        }

        public Func<object, DbContext> CreateDbContextCallback { get; set; }

        /// <summary>
        /// 将缓存中的键可转换为数据库中的键的回调。
        /// </summary>
        public Func<object, object> CacheKey2DbKeyCallback { get; set; }

        /// <summary>
        /// 将数据库中的键，转换为缓存键的回调。
        /// </summary>
        public Func<object, object> DbKey2CacheKeyCallback { get; set; }

        EfObjectCacheOptions IOptions<EfObjectCacheOptions>.Value => this;
    }

    /// <summary>
    /// 特定于使用EntityFrame框架管理的数据库对象的缓存类。
    /// </summary>
    public class EfObjectCache : DataObjectCache
    {
        public class EfObjectCacheEntry : DataObjectCacheEntry, IDisposable
        {
            #region 构造函数

            public EfObjectCacheEntry(object key, EfObjectCache cache) : base(key, cache)
            {
            }

            #endregion 构造函数

            /// <summary>
            /// 对象的类型。
            /// </summary>
            public Type ObjectType { get; set; }

            /// <summary>
            /// 管理该对象的上下文。如果没有设置则调用<see cref="EfObjectCacheOptions.CreateDbContextCallback"/>创建一个。
            /// </summary>
            public DbContext Context { get; set; }

            #region IDisposable接口相关

            /// <summary>
            /// 若没有设置则自动设置加载，创建，和保存等回调。
            /// <inheritdoc/>
            /// </summary>
            public override void Dispose()
            {
                if (ObjectType is null)
                    throw new InvalidCastException($"需要设置{nameof(ObjectType)}属性。");
                var options = (EfObjectCacheOptions)Cache.Options;
                if (Context is null)
                    Context = options.CreateDbContextCallback(Key);
                if (CreateCallback is null)
                    CreateCallback = (key, state) =>
                    {
                        return TypeDescriptor.CreateInstance(null, ObjectType, null, null);
                    };
                if (LoadCallback is null)
                    LoadCallback = (key, state) =>
                    {
                        return Context.Find(ObjectType, options.CacheKey2DbKeyCallback(key));
                    };
                if (SaveCallback is null)
                    SaveCallback = (obj, state) =>
                    {
                        Context.SaveChanges();
                        return true;
                    };

                base.Dispose();
            }

            #endregion IDisposable接口相关
        }

        public EfObjectCache(IOptions<EfObjectCacheOptions> options) : base(options)
        {
        }

        protected override MemoryCacheBaseEntry CreateEntryCore(object key)
        {
            return new EfObjectCacheEntry(key, this);
        }
    }
}
