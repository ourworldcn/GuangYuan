using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace GuangYuan.GY001.UserDb.Migrations
{
    public partial class _21091701 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GameBooty",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    PropertiesString = table.Column<string>(nullable: true),
                    ParentId = table.Column<Guid>(nullable: false),
                    CharId = table.Column<Guid>(nullable: false),
                    TemplateId = table.Column<Guid>(nullable: false),
                    Count = table.Column<decimal>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GameBooty", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PvpCombat",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    PropertiesString = table.Column<string>(nullable: true),
                    AttackerIdString = table.Column<string>(maxLength: 320, nullable: true),
                    DefenserIdString = table.Column<string>(maxLength: 320, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PvpCombat", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GameBooty_ParentId_CharId",
                table: "GameBooty",
                columns: new[] { "ParentId", "CharId" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GameBooty");

            migrationBuilder.DropTable(
                name: "PvpCombat");
        }
    }
}
