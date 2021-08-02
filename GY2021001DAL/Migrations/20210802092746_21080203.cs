using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace GuangYuan.GY001.UserDb.Migrations
{
    public partial class _21080203 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ActionRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    PropertiesString = table.Column<string>(nullable: true),
                    ParentId = table.Column<Guid>(nullable: false),
                    ActionId = table.Column<string>(maxLength: 64, nullable: true),
                    DateTimeUtc = table.Column<DateTime>(nullable: false),
                    Remark = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActionRecords", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ActionRecords_ActionId",
                table: "ActionRecords",
                column: "ActionId");

            migrationBuilder.CreateIndex(
                name: "IX_ActionRecords_DateTimeUtc",
                table: "ActionRecords",
                column: "DateTimeUtc");

            migrationBuilder.CreateIndex(
                name: "IX_ActionRecords_ParentId",
                table: "ActionRecords",
                column: "ParentId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ActionRecords");
        }
    }
}
