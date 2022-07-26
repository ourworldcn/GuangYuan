using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace GuangYuan.GY001.UserDb.Migrations
{
    public partial class _22082601 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GameBooty");

            migrationBuilder.DropTable(
                name: "WarNewspaper");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GameBooty",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CharId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    JsonObjectString = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ParentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PropertiesString = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GameBooty", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WarNewspaper",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AttackerExInfo = table.Column<byte[]>(type: "varbinary(max)", nullable: true),
                    AttackerIdString = table.Column<string>(type: "nvarchar(320)", maxLength: 320, nullable: true),
                    DefenserExInfo = table.Column<byte[]>(type: "varbinary(max)", nullable: true),
                    DefenserIdString = table.Column<string>(type: "nvarchar(320)", maxLength: 320, nullable: true),
                    EndUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    JsonObjectString = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PropertiesString = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    StartUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WarNewspaper", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GameBooty_ParentId_CharId",
                table: "GameBooty",
                columns: new[] { "ParentId", "CharId" });
        }
    }
}
