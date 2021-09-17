using GuangYuan.GY001.UserDb.Combat;
using Microsoft.EntityFrameworkCore;
using OW.Game;
using OW.Game.Store;
using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace GuangYuan.GY001.UserDb
{

    public class GY001UserContext : GameUserContext
    {

        public GY001UserContext()
        {

        }

        public GY001UserContext(DbContextOptions<GY001UserContext> dbContextOptions) : base(dbContextOptions)
        {

        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            //用户
            modelBuilder.Entity<GameUser>().HasIndex(c => c.LoginName).IsUnique(true);
            modelBuilder.Entity<GameUser>().HasIndex(c => c.CreateUtc).IsUnique(false);
            //角色
            modelBuilder.Entity<GameChar>().HasIndex(c => c.DisplayName).IsUnique(true);

            //物品
            modelBuilder.Entity<GameItem>().HasIndex(c => c.OwnerId);
            modelBuilder.Entity<GameItem>().HasIndex(c => new { c.TemplateId, c.Count }).IsUnique(false);
            modelBuilder.Entity<GameItem>().HasIndex(c => c.Count).IsUnique(false);

            //通用扩展属性
            modelBuilder.Entity<GameExtendProperty>().HasKey(c => new { c.Id, c.Name });

            //邮件相关
            modelBuilder.Entity<GameMailAddress>().HasIndex(c => c.ThingId).IsUnique(false);

            //社交关系
            modelBuilder.Entity<GameSocialRelationship>().HasKey(c => new { c.Id, c.Id2, c.KeyType });
            modelBuilder.Entity<GameSocialRelationship>().HasIndex(c => c.KeyType).IsUnique(false);
            modelBuilder.Entity<GameSocialRelationship>().HasIndex(c => c.Flag).IsUnique(false);
            modelBuilder.Entity<GameSocialRelationship>().HasIndex(c => c.PropertyString).IsUnique(false);


            //操作记录
            modelBuilder.Entity<GameActionRecord>().HasIndex(c => new { c.DateTimeUtc, c.ParentId, c.ActionId }).IsUnique(false);
            modelBuilder.Entity<GameActionRecord>().HasIndex(c => new { c.ParentId, c.ActionId }).IsUnique(false);
            modelBuilder.Entity<GameActionRecord>().HasIndex(c => c.ActionId).IsUnique(false);

            //角色特定的扩展属性
            modelBuilder.Entity<CharSpecificExpandProperty>().HasIndex(c => c.State).IsUnique(false);
            modelBuilder.Entity<CharSpecificExpandProperty>().HasIndex(c => c.PveCScore).IsUnique(false);
            modelBuilder.Entity<CharSpecificExpandProperty>().HasIndex(c => c.PvpScore).IsUnique(false);
            modelBuilder.Entity<CharSpecificExpandProperty>().HasIndex(c => c.PveTScore).IsUnique(false);
            modelBuilder.Entity<CharSpecificExpandProperty>().HasIndex(c => c.LastLogoutUtc).IsUnique(false);

            //战利品
            modelBuilder.Entity<GameBooty>().HasIndex(c => new { c.ParentId, c.CharId }).IsUnique(false);


            //调用基类方法。
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
        /// 游戏服务器用通用属性记录表。
        /// </summary>
        public DbSet<GameExtendProperty> ExtendProperties { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public DbSet<GameMail> Mails { get; set; }

        /// <summary>
        /// 社交关系对象。
        /// </summary>
        public DbSet<GameSocialRelationship> SocialRelationships { get; set; }

        /// <summary>
        /// 操作记录。
        /// </summary>
        public DbSet<GameActionRecord> ActionRecords { get; set; }

        /// <summary>
        /// pvp战斗记录。
        /// </summary>
        public DbSet<PvpCombat> PvpCombat { get; set; }

        /// <summary>
        /// 战利品记录。
        /// </summary>
        public DbSet<GameBooty> GameBooty { get; set; }

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
