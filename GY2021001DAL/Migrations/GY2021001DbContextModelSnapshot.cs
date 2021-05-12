﻿// <auto-generated />
using System;
using GY2021001DAL;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace GY2021001DAL.Migrations
{
    [DbContext(typeof(GY2021001DbContext))]
    partial class GY2021001DbContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "3.1.14")
                .HasAnnotation("Relational:MaxIdentifierLength", 128)
                .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

            modelBuilder.Entity("GY2021001DAL.GameChar", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("ClientGutsString")
                        .HasColumnType("nvarchar(max)");

                    b.Property<DateTime?>("CombatStartUtc")
                        .HasColumnType("datetime2");

                    b.Property<DateTime>("CreateUtc")
                        .HasColumnType("datetime2");

                    b.Property<Guid?>("CurrentDungeonId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("DisplayName")
                        .HasColumnType("nvarchar(max)");

                    b.Property<Guid>("GameUserId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("PropertiesString")
                        .HasColumnType("nvarchar(max)");

                    b.Property<Guid>("TemplateId")
                        .HasColumnType("uniqueidentifier");

                    b.HasKey("Id");

                    b.HasIndex("GameUserId");

                    b.ToTable("GameChar");
                });

            modelBuilder.Entity("GY2021001DAL.GameExtendProperty", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("Name")
                        .HasColumnType("nvarchar(64)")
                        .HasMaxLength(64);

                    b.Property<Guid>("ParentId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("Value")
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("Id");

                    b.HasIndex("ParentId");

                    b.ToTable("ExtendProperties");
                });

            modelBuilder.Entity("GY2021001DAL.GameItem", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("ClientGutsString")
                        .HasColumnType("nvarchar(max)");

                    b.Property<decimal?>("Count")
                        .HasColumnType("decimal(18,2)");

                    b.Property<DateTime>("CreateUtc")
                        .HasColumnType("datetime2");

                    b.Property<Guid?>("OwnerId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<Guid?>("ParentId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("PropertiesString")
                        .HasColumnType("nvarchar(max)");

                    b.Property<Guid>("TemplateId")
                        .HasColumnType("uniqueidentifier");

                    b.HasKey("Id");

                    b.HasIndex("OwnerId");

                    b.HasIndex("ParentId");

                    b.ToTable("GameItems");
                });

            modelBuilder.Entity("GY2021001DAL.GameSetting", b =>
                {
                    b.Property<string>("Name")
                        .HasColumnType("nvarchar(450)");

                    b.Property<string>("Val")
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("Name");

                    b.ToTable("GameSettings");
                });

            modelBuilder.Entity("GY2021001DAL.GameUser", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<DateTime>("CreateUtc")
                        .HasColumnType("datetime2");

                    b.Property<string>("LoginName")
                        .IsRequired()
                        .HasColumnType("nvarchar(64)")
                        .HasMaxLength(64);

                    b.Property<byte[]>("PwdHash")
                        .HasColumnType("varbinary(max)");

                    b.Property<string>("Region")
                        .HasColumnType("nvarchar(64)")
                        .HasMaxLength(64);

                    b.Property<Guid>("TemplateId")
                        .HasColumnType("uniqueidentifier");

                    b.HasKey("Id");

                    b.HasIndex("CreateUtc");

                    b.HasIndex("LoginName")
                        .IsUnique();

                    b.ToTable("GameUsers");
                });

            modelBuilder.Entity("GY2021001DAL.GameChar", b =>
                {
                    b.HasOne("GY2021001DAL.GameUser", "GameUser")
                        .WithMany("GameChars")
                        .HasForeignKey("GameUserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("GY2021001DAL.GameItem", b =>
                {
                    b.HasOne("GY2021001DAL.GameItem", "Parent")
                        .WithMany("Children")
                        .HasForeignKey("ParentId");
                });
#pragma warning restore 612, 618
        }
    }
}
