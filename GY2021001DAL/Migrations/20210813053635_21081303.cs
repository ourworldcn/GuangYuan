using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace GuangYuan.GY001.UserDb.Migrations
{
    public partial class _21081303 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "IdMarks");

            migrationBuilder.AddColumn<byte[]>(
                name: "ReceivedCharIds",
                table: "GameMailAttachment",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ReceivedCharIds",
                table: "GameMailAttachment");

            migrationBuilder.CreateTable(
                name: "IdMarks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ParentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PropertiesString = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IdMarks", x => new { x.Id, x.ParentId });
                });

            migrationBuilder.CreateIndex(
                name: "IX_IdMarks_ParentId",
                table: "IdMarks",
                column: "ParentId");
        }
    }
}
