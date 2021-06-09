﻿// <auto-generated />
using System;
using Gy2021001Template;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Gy2021001Template.Migrations
{
    [DbContext(typeof(GameTemplateContext))]
    partial class GameTemplateContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "3.1.14")
                .HasAnnotation("Relational:MaxIdentifierLength", 128)
                .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

            modelBuilder.Entity("Gy2021001Template.BlueprintTemplate", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
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

            modelBuilder.Entity("Gy2021001Template.BpFormulaTemplate", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
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

            modelBuilder.Entity("Gy2021001Template.BpItemTemplate", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

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

            modelBuilder.Entity("Gy2021001Template.DungeonLimit", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<Guid>("DungeonId")
                        .HasColumnName("关卡Id")
                        .HasColumnType("uniqueidentifier");

                    b.Property<int>("GroupNumber")
                        .HasColumnName("组号")
                        .HasColumnType("int");

                    b.Property<Guid>("ItemTemplateId")
                        .HasColumnName("物品Id")
                        .HasColumnType("uniqueidentifier");

                    b.Property<decimal>("MaxCount")
                        .HasColumnName("最大掉落数量")
                        .HasColumnType("decimal(18,2)");

                    b.Property<decimal>("MaxCountOfGroup")
                        .HasColumnName("本组最大掉落数量")
                        .HasColumnType("decimal(18,2)");

                    b.Property<string>("Remark")
                        .HasColumnName("备注")
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("Id");

                    b.ToTable("掉落限制");
                });

            modelBuilder.Entity("Gy2021001Template.GameItemTemplate", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
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

            modelBuilder.Entity("Gy2021001Template.BpFormulaTemplate", b =>
                {
                    b.HasOne("Gy2021001Template.BlueprintTemplate", "BlueprintTemplate")
                        .WithMany("FormulaTemplates")
                        .HasForeignKey("BlueprintTemplateId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("Gy2021001Template.BpItemTemplate", b =>
                {
                    b.HasOne("Gy2021001Template.BpFormulaTemplate", "FormulaTemplate")
                        .WithMany("BptfItemTemplates")
                        .HasForeignKey("BlueprintTemplateId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });
#pragma warning restore 612, 618
        }
    }
}
