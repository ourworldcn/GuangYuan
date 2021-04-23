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
                .HasAnnotation("ProductVersion", "2.2.6-servicing-10079")
                .HasAnnotation("Relational:MaxIdentifierLength", 128)
                .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

            modelBuilder.Entity("GY2021001DAL.GameChar", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<DateTime>("CreateUtc");

                    b.Property<Guid>("GameUserId");

                    b.Property<string>("PropertiesString");

                    b.Property<Guid>("TemplateId");

                    b.HasKey("Id");

                    b.HasIndex("GameUserId");

                    b.ToTable("GameChar");
                });

            modelBuilder.Entity("GY2021001DAL.GameItem", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<decimal?>("Count");

                    b.Property<DateTime>("CreateUtc");

                    b.Property<Guid?>("ParentId");

                    b.Property<string>("PropertiesString");

                    b.Property<Guid>("TemplateId");

                    b.Property<Guid?>("UserId");

                    b.HasKey("Id");

                    b.HasIndex("ParentId");

                    b.HasIndex("UserId");

                    b.ToTable("GameItems");
                });

            modelBuilder.Entity("GY2021001DAL.GameSetting", b =>
                {
                    b.Property<string>("Name")
                        .ValueGeneratedOnAdd();

                    b.Property<string>("Val");

                    b.HasKey("Name");

                    b.ToTable("GameSettings");
                });

            modelBuilder.Entity("GY2021001DAL.GameUser", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<DateTime>("CreateUtc");

                    b.Property<string>("LoginName")
                        .IsRequired()
                        .HasMaxLength(64);

                    b.Property<byte[]>("PwdHash");

                    b.Property<string>("Region")
                        .HasMaxLength(64);

                    b.Property<Guid>("TemplateId");

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
                        .OnDelete(DeleteBehavior.Cascade);
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
