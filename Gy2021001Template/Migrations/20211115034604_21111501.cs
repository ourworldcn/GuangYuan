using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace GuangYuan.GY001.TemplateDb.Migrations
{
    public partial class _21111501 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ShoppingTemplates",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    PropertiesString = table.Column<string>(nullable: true),
                    备注 = table.Column<string>(nullable: true),
                    DisplayName = table.Column<string>(nullable: true),
                    ChildrenTemplateIdString = table.Column<string>(nullable: true),
                    Genus = table.Column<string>(maxLength: 64, nullable: true),
                    GroupNumber = table.Column<int>(nullable: true),
                    ItemTemplateId = table.Column<Guid>(nullable: false),
                    AutoUse = table.Column<bool>(nullable: false),
                    StartDateTime = table.Column<DateTime>(nullable: false),
                    SellPeriod = table.Column<string>(nullable: true),
                    MaxCount = table.Column<decimal>(nullable: false),
                    ValidPeriod = table.Column<DateTime>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShoppingTemplates", x => x.Id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ShoppingTemplates");
        }
    }
}
