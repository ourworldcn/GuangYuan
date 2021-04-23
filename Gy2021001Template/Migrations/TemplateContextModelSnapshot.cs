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
    partial class TemplateContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
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

                    b.Property<int?>("GId");

                    b.Property<string>("GenusIdString");

                    b.Property<string>("PropertiesString");

                    b.Property<string>("SlotTemplateIdsString");

                    b.HasKey("Id");

                    b.ToTable("ItemTemplates");
                });
#pragma warning restore 612, 618
        }
    }
}
