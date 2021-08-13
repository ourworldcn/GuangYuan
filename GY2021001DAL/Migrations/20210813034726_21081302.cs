using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace GuangYuan.GY001.UserDb.Migrations
{
    public partial class _21081302 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EntityRelationship",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    Id2 = table.Column<Guid>(nullable: false),
                    Flag = table.Column<long>(nullable: false),
                    PropertyString = table.Column<string>(maxLength: 64, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EntityRelationship", x => new { x.Id, x.Id2, x.Flag });
                });

            migrationBuilder.CreateIndex(
                name: "IX_EntityRelationship_PropertyString",
                table: "EntityRelationship",
                column: "PropertyString");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EntityRelationship");
        }
    }
}
