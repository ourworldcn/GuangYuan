using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace GuangYuan.GY001.UserDb.Migrations
{
    public partial class _21082001 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CharSpecificExpandProperty_LastLogout",
                table: "CharSpecificExpandProperty");

            migrationBuilder.DropColumn(
                name: "LastLogout",
                table: "CharSpecificExpandProperty");

            migrationBuilder.AddColumn<DateTime>(
                name: "LastLogoutUtc",
                table: "CharSpecificExpandProperty",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.CreateIndex(
                name: "IX_CharSpecificExpandProperty_LastLogoutUtc",
                table: "CharSpecificExpandProperty",
                column: "LastLogoutUtc");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CharSpecificExpandProperty_LastLogoutUtc",
                table: "CharSpecificExpandProperty");

            migrationBuilder.DropColumn(
                name: "LastLogoutUtc",
                table: "CharSpecificExpandProperty");

            migrationBuilder.AddColumn<DateTime>(
                name: "LastLogout",
                table: "CharSpecificExpandProperty",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_CharSpecificExpandProperty_LastLogout",
                table: "CharSpecificExpandProperty",
                column: "LastLogout");
        }
    }
}
