using Microsoft.EntityFrameworkCore;
using System;
using System.ComponentModel.DataAnnotations;

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
            base.OnModelCreating(modelBuilder);
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


    }

    public static class MigrateDbInitializer
    {
        public static void Initialize(GY2021001DbContext context)
        {
            context.Database.Migrate();
        }
    }

}
