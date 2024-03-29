﻿// <auto-generated />
using System;
using GuangYuan.GY001.TemplateDb;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace GuangYuan.GY001.TemplateDb.Migrations
{
    [DbContext(typeof(GY001TemplateContext))]
    [Migration("20211115060112_21111503")]
    partial class _21111503
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "3.1.16")
                .HasAnnotation("Relational:MaxIdentifierLength", 128)
                .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

            modelBuilder.Entity("GuangYuan.GY001.TemplateDb.BlueprintTemplate", b =>
                {
                    b.Property<Guid>("Id")
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("ChildrenTemplateIdString")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("DisplayName")
                        .HasColumnType("nvarchar(max)");

                    b.Property<int?>("GId")
                        .HasColumnType("int");

                    b.Property<string>("PropertiesString")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Remark")
                        .HasColumnName("备注")
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("Id");

                    b.ToTable("蓝图");
                });

            modelBuilder.Entity("GuangYuan.GY001.TemplateDb.BpFormulaTemplate", b =>
                {
                    b.Property<Guid>("Id")
                        .HasColumnType("uniqueidentifier");

                    b.Property<Guid>("BlueprintTemplateId")
                        .HasColumnName("蓝图Id")
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("ChildrenTemplateIdString")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("DisplayName")
                        .HasColumnType("nvarchar(max)");

                    b.Property<bool>("IsContinue")
                        .HasColumnName("命中并继续")
                        .HasColumnType("bit");

                    b.Property<int>("OrderNumber")
                        .HasColumnName("序号")
                        .HasColumnType("int");

                    b.Property<string>("Prob")
                        .HasColumnName("命中概率")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("PropertiesString")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Remark")
                        .HasColumnName("备注")
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("Id");

                    b.HasIndex("BlueprintTemplateId");

                    b.ToTable("公式");
                });

            modelBuilder.Entity("GuangYuan.GY001.TemplateDb.BpItemTemplate", b =>
                {
                    b.Property<Guid>("Id")
                        .HasColumnType("uniqueidentifier");

                    b.Property<bool>("AllowEmpty")
                        .HasColumnName("允许空")
                        .HasColumnType("bit");

                    b.Property<Guid>("BlueprintTemplateId")
                        .HasColumnName("公式Id")
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("ChildrenTemplateIdString")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Conditional")
                        .HasColumnName("条件属性")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("CountLowerBound")
                        .HasColumnName("增量下限")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("CountProb")
                        .HasColumnName("增量概率")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("CountUpperBound")
                        .HasColumnName("增量上限")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("DisplayName")
                        .HasColumnType("nvarchar(max)");

                    b.Property<bool>("IsCountRound")
                        .HasColumnName("增量取整")
                        .HasColumnType("bit");

                    b.Property<bool>("IsNew")
                        .HasColumnName("新建物品否")
                        .HasColumnType("bit");

                    b.Property<string>("PropertiesChanges")
                        .HasColumnName("属性更改")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("PropertiesString")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Remark")
                        .HasColumnName("备注")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("VariableDeclaration")
                        .HasColumnName("变量声明")
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("Id");

                    b.HasIndex("BlueprintTemplateId");

                    b.ToTable("物料");
                });

            modelBuilder.Entity("GuangYuan.GY001.TemplateDb.GameItemTemplate", b =>
                {
                    b.Property<Guid>("Id")
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("ChildrenTemplateIdString")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("DisplayName")
                        .HasColumnType("nvarchar(max)");

                    b.Property<int?>("GId")
                        .HasColumnType("int");

                    b.Property<string>("GenusIdString")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("PropertiesString")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Remark")
                        .HasColumnName("备注")
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("Id");

                    b.ToTable("ItemTemplates");
                });

            modelBuilder.Entity("GuangYuan.GY001.TemplateDb.GamePropertyTemplate", b =>
                {
                    b.Property<string>("PName")
                        .HasColumnType("nvarchar(64)")
                        .HasMaxLength(64);

                    b.Property<string>("DefaultValue")
                        .HasColumnName("默认值")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("FName")
                        .HasColumnType("nvarchar(max)");

                    b.Property<bool>("HideToClient")
                        .HasColumnName("隐藏")
                        .HasColumnType("bit");

                    b.Property<string>("IndexBy")
                        .HasColumnName("索引属性名")
                        .HasColumnType("nvarchar(max)");

                    b.Property<bool>("IsFix")
                        .HasColumnName("不变")
                        .HasColumnType("bit");

                    b.Property<bool>("IsPrefix")
                        .HasColumnName("前缀")
                        .HasColumnType("bit");

                    b.Property<string>("Remark")
                        .HasColumnName("备注")
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("PName");

                    b.ToTable("动态属性元数据");
                });

            modelBuilder.Entity("GuangYuan.GY001.TemplateDb.GameShoppingTemplate", b =>
                {
                    b.Property<Guid>("Id")
                        .HasColumnType("uniqueidentifier");

                    b.Property<bool>("AutoUse")
                        .HasColumnType("bit");

                    b.Property<string>("Genus")
                        .HasColumnType("nvarchar(64)")
                        .HasMaxLength(64);

                    b.Property<int?>("GroupNumber")
                        .HasColumnType("int");

                    b.Property<Guid>("ItemTemplateId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<decimal>("MaxCount")
                        .HasColumnType("decimal(18,2)");

                    b.Property<string>("PropertiesString")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Remark")
                        .HasColumnName("备注")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("SellPeriod")
                        .HasColumnType("nvarchar(max)");

                    b.Property<DateTime>("StartDateTime")
                        .HasColumnType("datetime2");

                    b.Property<string>("ValidPeriod")
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("Id");

                    b.ToTable("ShoppingTemplates");
                });

            modelBuilder.Entity("GuangYuan.GY001.TemplateDb.BpFormulaTemplate", b =>
                {
                    b.HasOne("GuangYuan.GY001.TemplateDb.BlueprintTemplate", "BlueprintTemplate")
                        .WithMany("FormulaTemplates")
                        .HasForeignKey("BlueprintTemplateId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("GuangYuan.GY001.TemplateDb.BpItemTemplate", b =>
                {
                    b.HasOne("GuangYuan.GY001.TemplateDb.BpFormulaTemplate", "FormulaTemplate")
                        .WithMany("BptfItemTemplates")
                        .HasForeignKey("BlueprintTemplateId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });
#pragma warning restore 612, 618
        }
    }
}
