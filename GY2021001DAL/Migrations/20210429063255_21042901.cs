using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace GY2021001DAL.Migrations
{
    public partial class _21042901 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CombatStartUtc",
                table: "GameChar",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CurrentDungeonId",
                table: "GameChar",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CombatStartUtc",
                table: "GameChar");

            migrationBuilder.DropColumn(
                name: "CurrentDungeonId",
                table: "GameChar");
        }
    }
}
