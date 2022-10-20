using Microsoft.EntityFrameworkCore;
using OW.Game.Store;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

namespace Game.Logging
{
    public class GameLoggingDbContext : GameUserContext
    {
        public GameLoggingDbContext()
        {

        }

        public GameLoggingDbContext([NotNull] DbContextOptions options) : base(options)
        {
        }

        /// <summary>
        /// 存储付费订单信息。
        /// </summary>
        public DbSet<PayOrder> PayOrders { get; set; }

        public override void Dispose()
        {
            base.Dispose();
        }

        public override ValueTask DisposeAsync()
        {
            return base.DisposeAsync();
        }
    }

    public static class GameLoggingMigrateDbInitializer
    {
        public static void Initialize(GameLoggingDbContext context)
        {
            context.Database.Migrate();
        }
    }

}
