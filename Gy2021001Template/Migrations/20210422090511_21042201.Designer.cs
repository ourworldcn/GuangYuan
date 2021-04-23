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
    [Migration("20210422090511_21042201")]
    partial class _21042201
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "2.2.6-servicing-10079")
                .HasAnnotation("Relational:MaxIdentifierLength", 128)
                .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

            modelBuilder.Entity("Gy2021001Template.GameItemTemplate", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<string>("DisplayName");

                    b.Property<int>("GId");

                    b.Property<string>("PropertiesString");

                    b.HasKey("Id");

                    b.ToTable("ItemTemplates");
                });

            modelBuilder.Entity("Gy2021001Template.GameSlotTemplate", b =>
                {
                    b.Property<string>("Name")
                        .ValueGeneratedOnAdd()
                        .HasMaxLength(64);

                    b.HasKey("Name");

                    b.ToTable("SlotTemplates");
                });
#pragma warning restore 612, 618
        }
    }
}
