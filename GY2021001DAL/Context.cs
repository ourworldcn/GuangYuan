using Microsoft.EntityFrameworkCore;
using OW.Game;
using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace GuangYuan.GY001.UserDb
{

    public class GY001UserContext : DbContext
    {

        public GY001UserContext()
        {

        }

        public GY001UserContext(DbContextOptions<GY001UserContext> dbContextOptions) : base(dbContextOptions)
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

            //
            modelBuilder.Entity<GameMailAddress>().HasIndex(c => c.ThingId).IsUnique(false);
            modelBuilder.Entity<IdMark>().HasIndex(c => c.ParentId).IsUnique(false);
            modelBuilder.Entity<IdMark>().HasKey(c => new { c.Id, c.ParentId });

            //社交关系
            modelBuilder.Entity<GameSocialRelationship>().HasKey(c => new { c.Id, c.ObjectId });
            modelBuilder.Entity<GameSocialRelationship>().HasIndex(c => c.ObjectId).IsUnique(false);
            modelBuilder.Entity<GameSocialRelationship>().HasIndex(c => c.Friendliness).IsUnique(false);
            //操作记录
            modelBuilder.Entity<GameActionRecord>().HasIndex(c => c.DateTimeUtc).IsUnique(false);
            modelBuilder.Entity<GameActionRecord>().HasIndex(c => c.ParentId).IsUnique(false);
            modelBuilder.Entity<GameActionRecord>().HasIndex(c => c.ActionId).IsUnique(false);

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
        public DbSet<GameExtendProperty> ExtendProperties { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public DbSet<GameMail> Mails { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public DbSet<GameMailAddress> MailAddress { get; set; }

        /// <summary>
        /// 标记一组Id。
        /// </summary>
        public DbSet<IdMark> IdMarks { get; set; }

        /// <summary>
        /// 社交关系对象。
        /// </summary>
        public DbSet<GameSocialRelationship> SocialRelationships { get; set; }

        /// <summary>
        /// 操作记录。
        /// </summary>
        public DbSet<GameActionRecord> ActionRecords { get; set; }

        public override int SaveChanges(bool acceptAllChangesOnSuccess)
        {
            PrepareSaving();
            return base.SaveChanges(acceptAllChangesOnSuccess);
        }

        public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
        {
            PrepareSaving();
            return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        }

        private void PrepareSaving()
        {
            //foreach (var item in ActionRecords.Local.OfType<IBeforeSave>())
            //    item.PrepareSaving(this);
            //foreach (var item in this.Local.OfType<IBeforeSave>())
            //    item.PrepareSaving(this);
            var coll = ChangeTracker.Entries().Select(c=>c.Entity).OfType<IBeforeSave>();
            foreach (var item in coll)
            {
                item.PrepareSaving(this);
            }
        }

        public override void Dispose()
        {
            base.Dispose();
        }

        public override ValueTask DisposeAsync()
        {
            return base.DisposeAsync();
        }
    }

    public static class MigrateDbInitializer
    {
        public static void Initialize(GY001UserContext context)
        {
            context.Database.Migrate();
        }
    }


}
