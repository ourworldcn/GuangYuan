using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace GuangYuan.GY001.UserDb.Migrations
{
    public partial class _21042501 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ActionRecords_DateTimeUtc",
                table: "ActionRecords");

            migrationBuilder.DropIndex(
                name: "IX_ActionRecords_ParentId",
                table: "ActionRecords");

            migrationBuilder.CreateTable(
                name: "Rankings",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    PropertiesString = table.Column<string>(nullable: true),
                    PvpScore = table.Column<int>(nullable: false),
                    PveTScore = table.Column<int>(nullable: false),
                    PveCScore = table.Column<int>(nullable: false),
                    State = table.Column<string>(maxLength: 64, nullable: true),
                    LastLogout = table.Column<DateTime>(nullable: true),
                    HomelandShow = table.Column<string>(maxLength: 192, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Rankings", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ActionRecords_ParentId_ActionId",
                table: "ActionRecords",
                columns: new[] { "ParentId", "ActionId" });

            migrationBuilder.CreateIndex(
                name: "IX_ActionRecords_DateTimeUtc_ParentId_ActionId",
                table: "ActionRecords",
                columns: new[] { "DateTimeUtc", "ParentId", "ActionId" });

            migrationBuilder.CreateIndex(
                name: "IX_Rankings_HomelandShow",
                table: "Rankings",
                column: "HomelandShow");

            migrationBuilder.CreateIndex(
                name: "IX_Rankings_LastLogout",
                table: "Rankings",
                column: "LastLogout");

            migrationBuilder.CreateIndex(
                name: "IX_Rankings_PveCScore",
                table: "Rankings",
                column: "PveCScore");

            migrationBuilder.CreateIndex(
                name: "IX_Rankings_PveTScore",
                table: "Rankings",
                column: "PveTScore");

            migrationBuilder.CreateIndex(
                name: "IX_Rankings_PvpScore",
                table: "Rankings",
                column: "PvpScore");

            migrationBuilder.CreateIndex(
                name: "IX_Rankings_State",
                table: "Rankings",
                column: "State");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Rankings");

            migrationBuilder.DropIndex(
                name: "IX_ActionRecords_ParentId_ActionId",
                table: "ActionRecords");

            migrationBuilder.DropIndex(
                name: "IX_ActionRecords_DateTimeUtc_ParentId_ActionId",
                table: "ActionRecords");

            migrationBuilder.CreateIndex(
                name: "IX_ActionRecords_DateTimeUtc",
                table: "ActionRecords",
                column: "DateTimeUtc");

            migrationBuilder.CreateIndex(
                name: "IX_ActionRecords_ParentId",
                table: "ActionRecords",
                column: "ParentId");
        }
    }
}
