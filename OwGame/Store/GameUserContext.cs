using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.ObjectPool;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Data.Common;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace OW.Game.Store
{
    /// <summary>
    /// 仅统计当前有多少命令在执行。大致可以反应数据库造成的IO压力。
    /// </summary>
    public class OwGameCommandInterceptor : DbCommandInterceptor
    {
        public static volatile int QueryExecutingCount;

        public static volatile int ReaderExecutingCount;

        public static volatile int ScalarExecutingCount;

        public static int ExecutingCount => QueryExecutingCount + ReaderExecutingCount + ScalarExecutingCount;

        /// <summary>
        /// 每当并发的操作数减少时会发出信号。
        /// </summary>
        public static AutoResetEvent ExecutingCountChanged = new AutoResetEvent(false);

        /// <summary>
        /// 构造函数。
        /// </summary>
        public OwGameCommandInterceptor()
        {
        }

        public override InterceptionResult<int> NonQueryExecuting(DbCommand command, CommandEventData eventData, InterceptionResult<int> result)
        {
            Interlocked.Increment(ref QueryExecutingCount);
            return base.NonQueryExecuting(command, eventData, result);
        }

        public override int NonQueryExecuted(DbCommand command, CommandExecutedEventData eventData, int result)
        {
            Interlocked.Decrement(ref QueryExecutingCount);
            ExecutingCountChanged.Set();
            return base.NonQueryExecuted(command, eventData, result);
        }

        public override InterceptionResult<DbDataReader> ReaderExecuting(DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result)
        {
            Interlocked.Increment(ref ReaderExecutingCount);
            return base.ReaderExecuting(command, eventData, result);
        }

        public override DbDataReader ReaderExecuted(DbCommand command, CommandExecutedEventData eventData, DbDataReader result)
        {
            Interlocked.Decrement(ref ReaderExecutingCount);
            ExecutingCountChanged.Set();
            return base.ReaderExecuted(command, eventData, result);
        }

        public override InterceptionResult<object> ScalarExecuting(DbCommand command, CommandEventData eventData, InterceptionResult<object> result)
        {
            Interlocked.Increment(ref ScalarExecutingCount);
            return base.ScalarExecuting(command, eventData, result);
        }

        public override object ScalarExecuted(DbCommand command, CommandExecutedEventData eventData, object result)
        {
            Interlocked.Decrement(ref ScalarExecutingCount);
            ExecutingCountChanged.Set();
            return base.ScalarExecuted(command, eventData, result);
        }
    }

    /// <summary>
    /// 游戏的玩家数据库上下文。
    /// </summary>
    /// <remarks>保存时会对跟踪的数据中支持<see cref="IBeforeSave"/>接口的对象调用<see cref="IBeforeSave.PrepareSaving(DbContext)"/></remarks>
    public class GameUserContext : DbContext
    {
        public GameUserContext([NotNull] DbContextOptions options) : base(options)
        {
        }

        protected GameUserContext()
        {
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.AddInterceptors(new OwGameCommandInterceptor());
            base.OnConfiguring(optionsBuilder);
        }

        #region 保存前

        public override int SaveChanges(bool acceptAllChangesOnSuccess)
        {
            PrepareSaving();
            return base.SaveChanges(acceptAllChangesOnSuccess);
        }

        public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default) =>
            Task.Run(() =>
            {
                PrepareSaving();
                return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
            });

        /// <summary>
        /// 在保存被调用。
        /// </summary>
        private void PrepareSaving()
        {
            var coll = ChangeTracker.Entries().Select(c => c.Entity).OfType<IBeforeSave>().Where(c => !c.SuppressSave).ToList();
            foreach (var item in coll)
            {
                try
                {
                    item.PrepareSaving(this);
                }
                catch (Exception err)
                {
                    Debug.WriteLine($"预保存时发生错误——{err.Message}");
                }
            }
        }
        #endregion 保存前
    }
}
