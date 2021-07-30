using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace GuangYuan.GY001.UserDb.Migrations
{
    public partial class _21072704 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GameMailAttachment",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    MailId = table.Column<Guid>(nullable: false),
                    PropertyString = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GameMailAttachment", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GameMailAttachment_Mails_MailId",
                        column: x => x.MailId,
                        principalTable: "Mails",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GameMailAttachment_MailId",
                table: "GameMailAttachment",
                column: "MailId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GameMailAttachment");
        }
    }
}
