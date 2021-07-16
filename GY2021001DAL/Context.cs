using Microsoft.EntityFrameworkCore;
using System;
using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;

namespace GY2021001DAL
{

    public class GY2021001DbContext : DbContext
    {

        public GY2021001DbContext()
        {

        }

        public GY2021001DbContext(DbContextOptions<GY2021001DbContext> dbContextOptions) : base(dbContextOptions)
        {

        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<GameUser>().HasIndex(c => c.CreateUtc);
            modelBuilder.Entity<GameUser>().HasIndex(c => c.LoginName).IsUnique();
            modelBuilder.Entity<GameItem>().HasIndex(c => c.OwnerId);
            modelBuilder.Entity<GameClientExtendProperty>().HasIndex(c => c.ParentId);
            //
            modelBuilder.Entity<GameExtendProperty>().HasKey(c => new { c.ParentId, c.Name });
            modelBuilder.Entity<GameExtendProperty>().HasIndex(c => c.Name).IsUnique(false);
            modelBuilder.Entity<GameExtendProperty>().HasIndex(c => c.DecimalValue).IsUnique(false);
            modelBuilder.Entity<GameExtendProperty>().HasIndex(c => c.DoubleValue).IsUnique(false);
            modelBuilder.Entity<GameExtendProperty>().HasIndex(c => c.IntValue).IsUnique(false);
            modelBuilder.Entity<GameExtendProperty>().HasIndex(c => c.StringValue).IsUnique(false);
            base.OnModelCreating(modelBuilder);
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            base.OnConfiguring(optionsBuilder);
        }

        /// <summary>
        /// 用户账号表。
        /// </summary>
        public DbSet<GameUser> GameUsers { get; set; }

        /// <summary>
        /// 角色表。
        /// </summary>
        public DbSet<GameChar> GameChars { get; set; }

        /// <summary>
        /// 物品/道具表
        /// </summary>
        public DbSet<GameItem> GameItems { get; set; }
        /// <summary>
        /// 全局动态设置表。
        /// </summary>
        public DbSet<GameSetting> GameSettings { get; set; }

        /// <summary>
        /// 客户端通用扩展属性记录的表。
        /// </summary>
        public DbSet<GameClientExtendProperty> ClientExtendProperties { get; set; }

        /// <summary>
        /// 游戏服务器用通用属性记录表。
        /// </summary>
        public DbSet<GameExtendProperty> GameExtendProperties { get; set; }

        public override int SaveChanges(bool acceptAllChangesOnSuccess)
        {
            foreach (var item in GameUsers.Local)
                item.CurrentChar.InvokeSaving(EventArgs.Empty);
            return base.SaveChanges(acceptAllChangesOnSuccess);
        }

        public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
        {
            foreach (var item in GameUsers.Local)
                item.CurrentChar.InvokeSaving(EventArgs.Empty);
            return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        }

    }

    public static class MigrateDbInitializer
    {
        public static void Initialize(GY2021001DbContext context)
        {
            context.Database.Migrate();
        }
    }

}
