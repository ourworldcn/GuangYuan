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
    [Migration("20210929104655_21092901")]
    partial class _21092901
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "3.1.16")
                .HasAnnotation("Relational:MaxIdentifierLength", 128)
                .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

            modelBuilder.Entity("GuangYuan.GY001.UserDb.CharSpecificExpandProperty", b =>
                {
                    b.Property<Guid>("Id")
                        .HasColumnType("uniqueidentifier");

                    b.Property<int>("CharLevel")
                        .HasColumnType("int");

                    b.Property<int>("FrinedCount")
                        .HasColumnType("int");

                    b.Property<int>("FrinedMaxCount")
                        .HasColumnType("int");

                    b.Property<DateTime>("LastLogoutUtc")
                        .HasColumnType("datetime2");

                    b.Property<int>("LastPvpScore")
                        .HasColumnType("int");

                    b.Property<string>("PropertiesString")
                        .HasColumnType("nvarchar(max)");

                    b.Property<int>("PveCScore")
                        .HasColumnType("int");

                    b.Property<int>("PveTScore")
                        .HasColumnType("int");

                    b.Property<int>("PvpScore")
                        .HasColumnType("int");

                    b.Property<string>("State")
                        .HasColumnType("nvarchar(64)")
                        .HasMaxLength(64);

                    b.HasKey("Id");

                    b.HasIndex("LastLogoutUtc");

                    b.HasIndex("PveCScore");

                    b.HasIndex("PveTScore");

                    b.HasIndex("PvpScore");

                    b.HasIndex("State");

                    b.ToTable("CharSpecificExpandProperty");
                });

            modelBuilder.Entity("GuangYuan.GY001.UserDb.Combat.GameBooty", b =>
                {
                    b.Property<Guid>("Id")
                        .HasColumnType("uniqueidentifier");

                    b.Property<Guid>("CharId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<decimal>("Count")
                        .HasColumnType("decimal(18,2)");

                    b.Property<Guid>("ParentId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("PropertiesString")
                        .HasColumnType("nvarchar(max)");

                    b.Property<Guid>("TemplateId")
                        .HasColumnType("uniqueidentifier");

                    b.HasKey("Id");

                    b.HasIndex("ParentId", "CharId");

                    b.ToTable("GameBooty");
                });

            modelBuilder.Entity("GuangYuan.GY001.UserDb.Combat.PvpCombat", b =>
                {
                    b.Property<Guid>("Id")
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("AttackerIdString")
                        .HasColumnType("nvarchar(320)")
                        .HasMaxLength(320);

                    b.Property<string>("DefenserIdString")
                        .HasColumnType("nvarchar(320)")
                        .HasMaxLength(320);

                    b.Property<string>("PropertiesString")
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("Id");

                    b.ToTable("PvpCombat");
                });

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

                    b.HasIndex("ParentId", "ActionId");

                    b.HasIndex("DateTimeUtc", "ParentId", "ActionId");

                    b.ToTable("ActionRecords");
                });

            modelBuilder.Entity("GuangYuan.GY001.UserDb.GameChar", b =>
                {
                    b.Property<Guid>("Id")
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
                        .HasColumnType("nvarchar(64)")
                        .HasMaxLength(64);

                    b.Property<Guid>("GameUserId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("PropertiesString")
                        .HasColumnType("nvarchar(max)");

                    b.Property<Guid>("TemplateId")
                        .HasColumnType("uniqueidentifier");

                    b.HasKey("Id");

                    b.HasIndex("DisplayName")
                        .IsUnique()
                        .HasFilter("[DisplayName] IS NOT NULL");

                    b.HasIndex("GameUserId");

                    b.ToTable("GameChars");
                });

            modelBuilder.Entity("GuangYuan.GY001.UserDb.GameExtendProperty", b =>
                {
                    b.Property<Guid>("Id")
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("Tag")
                        .HasColumnType("nvarchar(64)")
                        .HasMaxLength(64);

                    b.Property<DateTime>("DateTimeValue")
                        .HasColumnType("datetime2");

                    b.Property<decimal>("DecimalValue")
                        .HasColumnType("decimal(18,2)");

                    b.Property<double>("DoubleValue")
                        .HasColumnType("float");

                    b.Property<Guid?>("GuidValue")
                        .HasColumnType("uniqueidentifier");

                    b.Property<int>("IntValue")
                        .HasColumnType("int");

                    b.Property<string>("PropertiesString")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("StringValue")
                        .HasColumnType("nvarchar(256)")
                        .HasMaxLength(256);

                    b.Property<string>("Text")
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("Id", "Tag");

                    b.ToTable("ExtendProperties");
                });

            modelBuilder.Entity("GuangYuan.GY001.UserDb.GameItem", b =>
                {
                    b.Property<Guid>("Id")
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

                    b.HasIndex("Count");

                    b.HasIndex("OwnerId");

                    b.HasIndex("ParentId");

                    b.HasIndex("TemplateId", "Count");

                    b.ToTable("GameItems");
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

                    b.Property<string>("PropertiesString")
                        .HasColumnType("nvarchar(max)");

                    b.Property<Guid>("ThingId")
                        .HasColumnType("uniqueidentifier");

                    b.HasKey("Id");

                    b.HasIndex("MailId");

                    b.HasIndex("ThingId");

                    b.ToTable("MailAddresses");
                });

            modelBuilder.Entity("GuangYuan.GY001.UserDb.GameMailAttachment", b =>
                {
                    b.Property<Guid>("Id")
                        .HasColumnType("uniqueidentifier");

                    b.Property<Guid>("MailId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("PropertiesString")
                        .HasColumnType("nvarchar(max)");

                    b.Property<byte[]>("ReceivedCharIds")
                        .HasColumnType("varbinary(max)");

                    b.HasKey("Id");

                    b.HasIndex("MailId");

                    b.ToTable("MailAttachmentes");
                });

            modelBuilder.Entity("GuangYuan.GY001.UserDb.GameSetting", b =>
                {
                    b.Property<string>("Tag")
                        .HasColumnType("nvarchar(450)");

                    b.Property<string>("Val")
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("Tag");

                    b.ToTable("GameSettings");
                });

            modelBuilder.Entity("GuangYuan.GY001.UserDb.GameSocialRelationship", b =>
                {
                    b.Property<Guid>("Id")
                        .HasColumnType("uniqueidentifier");

                    b.Property<Guid>("Id2")
                        .HasColumnType("uniqueidentifier");

                    b.Property<int>("KeyType")
                        .HasColumnType("int");

                    b.Property<int>("Flag")
                        .HasColumnType("int");

                    b.Property<string>("PropertiesString")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("PropertyString")
                        .HasColumnType("nvarchar(64)")
                        .HasMaxLength(64);

                    b.HasKey("Id", "Id2", "KeyType");

                    b.HasIndex("Flag");

                    b.HasIndex("KeyType");

                    b.HasIndex("PropertyString");

                    b.ToTable("SocialRelationships");
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

                    b.Property<int?>("NodeNum")
                        .HasColumnType("int");

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

            modelBuilder.Entity("GuangYuan.GY001.UserDb.CharSpecificExpandProperty", b =>
                {
                    b.HasOne("GuangYuan.GY001.UserDb.GameChar", "GameChar")
                        .WithOne("SpecificExpandProperties")
                        .HasForeignKey("GuangYuan.GY001.UserDb.CharSpecificExpandProperty", "Id")
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
#pragma warning restore 612, 618
        }
    }
}
