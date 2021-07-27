using Microsoft.EntityFrameworkCore.Migrations;

namespace GY2021001DAL.Migrations
{
    public partial class _21072705 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_GameMailAddress_Mails_MailId",
                table: "GameMailAddress");

            migrationBuilder.DropPrimaryKey(
                name: "PK_GameMailAddress",
                table: "GameMailAddress");

            migrationBuilder.RenameTable(
                name: "GameMailAddress",
                newName: "MailAddress");

            migrationBuilder.RenameIndex(
                name: "IX_GameMailAddress_MailId",
                table: "MailAddress",
                newName: "IX_MailAddress_MailId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_MailAddress",
                table: "MailAddress",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_MailAddress_ThingId",
                table: "MailAddress",
                column: "ThingId");

            migrationBuilder.AddForeignKey(
                name: "FK_MailAddress_Mails_MailId",
                table: "MailAddress",
                column: "MailId",
                principalTable: "Mails",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MailAddress_Mails_MailId",
                table: "MailAddress");

            migrationBuilder.DropPrimaryKey(
                name: "PK_MailAddress",
                table: "MailAddress");

            migrationBuilder.DropIndex(
                name: "IX_MailAddress_ThingId",
                table: "MailAddress");

            migrationBuilder.RenameTable(
                name: "MailAddress",
                newName: "GameMailAddress");

            migrationBuilder.RenameIndex(
                name: "IX_MailAddress_MailId",
                table: "GameMailAddress",
                newName: "IX_GameMailAddress_MailId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_GameMailAddress",
                table: "GameMailAddress",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_GameMailAddress_Mails_MailId",
                table: "GameMailAddress",
                column: "MailId",
                principalTable: "Mails",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
