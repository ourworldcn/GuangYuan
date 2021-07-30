using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace GuangYuan.GY001.UserDb.Migrations
{
    public partial class _21072703 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "From",
                table: "Mails");

            migrationBuilder.DropColumn(
                name: "To",
                table: "Mails");

            migrationBuilder.AddColumn<DateTime>(
                name: "CreateUtc",
                table: "Mails",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "PropertyString",
                table: "Mails",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "GameMailAddress",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    DisplayName = table.Column<string>(nullable: true),
                    ThingId = table.Column<Guid>(nullable: false),
                    MailId = table.Column<Guid>(nullable: false),
                    Kind = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GameMailAddress", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GameMailAddress_Mails_MailId",
                        column: x => x.MailId,
                        principalTable: "Mails",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GameMailAddress_MailId",
                table: "GameMailAddress",
                column: "MailId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GameMailAddress");

            migrationBuilder.DropColumn(
                name: "CreateUtc",
                table: "Mails");

            migrationBuilder.DropColumn(
                name: "PropertyString",
                table: "Mails");

            migrationBuilder.AddColumn<string>(
                name: "From",
                table: "Mails",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "To",
                table: "Mails",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}
