using Microsoft.EntityFrameworkCore;
using System;
using System.ComponentModel.DataAnnotations;

namespace GuangYuan.GY001.TemplateDb
{
    public class GY001TemplateContext : DbContext
    {
        public GY001TemplateContext()
        {

        }

        public GY001TemplateContext(DbContextOptions options) : base(options)
        {

        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
        }

        ///// <summary>
        ///// 属性定义表。
        ///// </summary>
        //public DbSet<GamePropertyTemplate> PropertyTemplates { get; set; }

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
        public static void Initialize(GY001TemplateContext context)
        {
            context.Database.Migrate();
        }
    }

}
