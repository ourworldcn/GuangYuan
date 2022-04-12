using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace GuangYuan.GY001.UserDb.Migrations
{
    public partial class _22041201 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Guild",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    PropertiesString = table.Column<string>(nullable: true),
                    TemplateId = table.Column<Guid>(nullable: false),
                    ExtraString = table.Column<string>(maxLength: 64, nullable: true),
                    ExtraDecimal = table.Column<decimal>(nullable: true),
                    BinaryArray = table.Column<byte[]>(nullable: true),
                    DisplayName = table.Column<string>(maxLength: 64, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Guild", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Guild_DisplayName",
                table: "Guild",
                column: "DisplayName",
                unique: true,
                filter: "[DisplayName] IS NOT NULL");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Guild");
        }
    }
}
