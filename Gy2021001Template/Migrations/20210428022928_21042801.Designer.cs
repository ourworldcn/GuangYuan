﻿// <auto-generated />
using System;
using Gy2021001Template;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Gy2021001Template.Migrations
{
    [DbContext(typeof(GameTemplateContext))]
    [Migration("20210428022928_21042801")]
    partial class _21042801
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "3.1.14")
                .HasAnnotation("Relational:MaxIdentifierLength", 128)
                .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

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

                    b.HasKey("Id");

                    b.ToTable("ItemTemplates");
                });
#pragma warning restore 612, 618
        }
    }
}
