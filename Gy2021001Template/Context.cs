using Microsoft.EntityFrameworkCore;
using System;
using System.ComponentModel.DataAnnotations;

namespace Gy2021001Template
{
    public class GameTemplateContext : DbContext
    {
        public GameTemplateContext()
        {

        }

        public GameTemplateContext(DbContextOptions options) : base(options)
        {

        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
        }

        /// <summary>
        /// 装备表。
        /// </summary>
        public DbSet<GameItemTemplate> ItemTemplates { get; set; }

        /// <summary>
        /// 蓝图表
        /// </summary>
        public DbSet<BlueprintTemplate> BlueprintTemplates { get; set; }
    }

    public static class TemplateMigrateDbInitializer
    {
        public static void Initialize(GameTemplateContext context)
        {
            context.Database.Migrate();
        }
    }

}
