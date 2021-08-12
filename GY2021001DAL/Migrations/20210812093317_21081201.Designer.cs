﻿// <auto-generated />
using System;
using GuangYuan.GY001.UserDb;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace GuangYuan.GY001.UserDb.Migrations
{
    [DbContext(typeof(GY001UserContext))]
    [Migration("20210812093317_21081201")]
    partial class _21081201
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "3.1.16")
                .HasAnnotation("Relational:MaxIdentifierLength", 128)
                .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

            modelBuilder.Entity("GuangYuan.GY001.UserDb.GameActionRecord", b =>
                {
                    b.Property<Guid>("Id")
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("ActionId")
                        .HasColumnType("nvarchar(64)")
                        .HasMaxLength(64);

                    b.Property<DateTime>("DateTimeUtc")
                        .HasColumnType("datetime2");

                    b.Property<Guid>("ParentId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("PropertiesString")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Remark")
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("Id");

                    b.HasIndex("ActionId");

                    b.HasIndex("DateTimeUtc");

                    b.HasIndex("ParentId");

                    b.ToTable("ActionRecords");
                });

            modelBuilder.Entity("GuangYuan.GY001.UserDb.GameClientExtendProperty", b =>
                {
                    b.Property<Guid>("Id")
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

                    b.ToTable("ClientExtendProperties");
                });

            modelBuilder.Entity("GuangYuan.GY001.UserDb.GameExtendProperty", b =>
                {
                    b.Property<Guid>("ParentId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("Name")
                        .HasColumnType("nvarchar(64)")
                        .HasMaxLength(64);

                    b.Property<decimal>("DecimalValue")
                        .HasColumnType("decimal(18,2)");

                    b.Property<double>("DoubleValue")
                        .HasColumnType("float");

                    b.Property<int>("IntValue")
                        .HasColumnType("int");

                    b.Property<string>("StringValue")
                        .HasColumnType("nvarchar(256)")
                        .HasMaxLength(256);

                    b.Property<string>("Text")
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("ParentId", "Name");

                    b.HasIndex("DecimalValue");

                    b.HasIndex("DoubleValue");

                    b.HasIndex("IntValue");

                    b.HasIndex("Name");

                    b.HasIndex("StringValue");

                    b.ToTable("ExtendProperties");
                });

            modelBuilder.Entity("GuangYuan.GY001.UserDb.GameMail", b =>
                {
                    b.Property<Guid>("Id")
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("Body")
                        .HasColumnType("nvarchar(max)");

                    b.Property<DateTime>("CreateUtc")
                        .HasColumnType("datetime2");

                    b.Property<string>("PropertiesString")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Subject")
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("Id");

                    b.ToTable("Mails");
                });

            modelBuilder.Entity("GuangYuan.GY001.UserDb.GameMailAddress", b =>
                {
                    b.Property<Guid>("Id")
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("DisplayName")
                        .HasColumnType("nvarchar(max)");

                    b.Property<bool>("IsDeleted")
                        .HasColumnType("bit");

                    b.Property<int>("Kind")
                        .HasColumnType("int");

                    b.Property<Guid>("MailId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<Guid>("ThingId")
                        .HasColumnType("uniqueidentifier");

                    b.HasKey("Id");

                    b.HasIndex("MailId");

                    b.HasIndex("ThingId");

                    b.ToTable("MailAddress");
                });

            modelBuilder.Entity("GuangYuan.GY001.UserDb.GameMailAttachment", b =>
                {
                    b.Property<Guid>("Id")
                        .HasColumnType("uniqueidentifier");

                    b.Property<Guid>("MailId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("PropertiesString")
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("Id");

                    b.HasIndex("MailId");

                    b.ToTable("GameMailAttachment");
                });

            modelBuilder.Entity("GuangYuan.GY001.UserDb.GameSetting", b =>
                {
                    b.Property<string>("Name")
                        .HasColumnType("nvarchar(450)");

                    b.Property<string>("Val")
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("Name");

                    b.ToTable("GameSettings");
                });

            modelBuilder.Entity("GuangYuan.GY001.UserDb.GameSocialRelationship", b =>
                {
                    b.Property<Guid>("Id")
                        .HasColumnType("uniqueidentifier");

                    b.Property<Guid>("ObjectId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<short>("Friendliness")
                        .HasColumnType("smallint");

                    b.Property<string>("PropertiesString")
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("Id", "ObjectId");

                    b.HasIndex("Friendliness");

                    b.HasIndex("ObjectId");

                    b.ToTable("SocialRelationships");
                });

            modelBuilder.Entity("GuangYuan.GY001.UserDb.GameThingBase", b =>
                {
                    b.Property<Guid>("Id")
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("ClientGutsString")
                        .HasColumnType("nvarchar(max)");

                    b.Property<DateTime>("CreateUtc")
                        .HasColumnType("datetime2");

                    b.Property<string>("Discriminator")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("PropertiesString")
                        .HasColumnType("nvarchar(max)");

                    b.Property<Guid>("TemplateId")
                        .HasColumnType("uniqueidentifier");

                    b.HasKey("Id");

                    b.ToTable("GameThingBase");

                    b.HasDiscriminator<string>("Discriminator").HasValue("GameThingBase");
                });

            modelBuilder.Entity("GuangYuan.GY001.UserDb.GameUser", b =>
                {
                    b.Property<Guid>("Id")
                        .HasColumnType("uniqueidentifier");

                    b.Property<DateTime>("CreateUtc")
                        .HasColumnType("datetime2");

                    b.Property<string>("LoginName")
                        .IsRequired()
                        .HasColumnType("nvarchar(64)")
                        .HasMaxLength(64);

                    b.Property<string>("PropertiesString")
                        .HasColumnType("nvarchar(max)");

                    b.Property<byte[]>("PwdHash")
                        .HasColumnType("varbinary(max)");

                    b.Property<string>("Region")
                        .HasColumnType("nvarchar(64)")
                        .HasMaxLength(64);

                    b.HasKey("Id");

                    b.HasIndex("CreateUtc");

                    b.HasIndex("LoginName")
                        .IsUnique();

                    b.ToTable("GameUsers");
                });

            modelBuilder.Entity("GuangYuan.GY001.UserDb.IdMark", b =>
                {
                    b.Property<Guid>("Id")
                        .HasColumnType("uniqueidentifier");

                    b.Property<Guid>("ParentId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("PropertiesString")
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("Id", "ParentId");

                    b.HasIndex("ParentId");

                    b.ToTable("IdMarks");
                });

            modelBuilder.Entity("GuangYuan.GY001.UserDb.GameChar", b =>
                {
                    b.HasBaseType("GuangYuan.GY001.UserDb.GameThingBase");

                    b.Property<DateTime?>("CombatStartUtc")
                        .HasColumnType("datetime2");

                    b.Property<Guid?>("CurrentDungeonId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("DisplayName")
                        .HasColumnType("nvarchar(450)");

                    b.Property<Guid>("GameUserId")
                        .HasColumnType("uniqueidentifier");

                    b.HasIndex("DisplayName")
                        .IsUnique()
                        .HasFilter("[DisplayName] IS NOT NULL");

                    b.HasIndex("GameUserId");

                    b.HasDiscriminator().HasValue("GameChar");
                });

            modelBuilder.Entity("GuangYuan.GY001.UserDb.GameItem", b =>
                {
                    b.HasBaseType("GuangYuan.GY001.UserDb.GameThingBase");

                    b.Property<decimal?>("Count")
                        .HasColumnType("decimal(18,2)");

                    b.Property<Guid?>("OwnerId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<Guid?>("ParentId")
                        .HasColumnType("uniqueidentifier");

                    b.HasIndex("OwnerId");

                    b.HasIndex("ParentId");

                    b.HasDiscriminator().HasValue("GameItem");
                });

            modelBuilder.Entity("GuangYuan.GY001.UserDb.GameExtendProperty", b =>
                {
                    b.HasOne("GuangYuan.GY001.UserDb.GameThingBase", "GameThing")
                        .WithMany("ExtendProperties")
                        .HasForeignKey("ParentId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("GuangYuan.GY001.UserDb.GameMailAddress", b =>
                {
                    b.HasOne("GuangYuan.GY001.UserDb.GameMail", "Mail")
                        .WithMany("Addresses")
                        .HasForeignKey("MailId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("GuangYuan.GY001.UserDb.GameMailAttachment", b =>
                {
                    b.HasOne("GuangYuan.GY001.UserDb.GameMail", "Mail")
                        .WithMany("Attachmentes")
                        .HasForeignKey("MailId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("GuangYuan.GY001.UserDb.GameChar", b =>
                {
                    b.HasOne("GuangYuan.GY001.UserDb.GameUser", "GameUser")
                        .WithMany("GameChars")
                        .HasForeignKey("GameUserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("GuangYuan.GY001.UserDb.GameItem", b =>
                {
                    b.HasOne("GuangYuan.GY001.UserDb.GameItem", "Parent")
                        .WithMany("Children")
                        .HasForeignKey("ParentId");
                });
#pragma warning restore 612, 618
        }
    }
}