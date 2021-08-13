using Microsoft.EntityFrameworkCore.Migrations;

namespace GuangYuan.GY001.UserDb.Migrations
{
    public partial class _21081304 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_GameMailAttachment_Mails_MailId",
                table: "GameMailAttachment");

            migrationBuilder.DropForeignKey(
                name: "FK_MailAddress_Mails_MailId",
                table: "MailAddress");

            migrationBuilder.DropPrimaryKey(
                name: "PK_MailAddress",
                table: "MailAddress");

            migrationBuilder.DropPrimaryKey(
                name: "PK_GameMailAttachment",
                table: "GameMailAttachment");

            migrationBuilder.RenameTable(
                name: "MailAddress",
                newName: "MailAddresses");

            migrationBuilder.RenameTable(
                name: "GameMailAttachment",
                newName: "MailAttachmentes");

            migrationBuilder.RenameIndex(
                name: "IX_MailAddress_ThingId",
                table: "MailAddresses",
                newName: "IX_MailAddresses_ThingId");

            migrationBuilder.RenameIndex(
                name: "IX_MailAddress_MailId",
                table: "MailAddresses",
                newName: "IX_MailAddresses_MailId");

            migrationBuilder.RenameIndex(
                name: "IX_GameMailAttachment_MailId",
                table: "MailAttachmentes",
                newName: "IX_MailAttachmentes_MailId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_MailAddresses",
                table: "MailAddresses",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_MailAttachmentes",
                table: "MailAttachmentes",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_MailAddresses_Mails_MailId",
                table: "MailAddresses",
                column: "MailId",
                principalTable: "Mails",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_MailAttachmentes_Mails_MailId",
                table: "MailAttachmentes",
                column: "MailId",
                principalTable: "Mails",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MailAddresses_Mails_MailId",
                table: "MailAddresses");

            migrationBuilder.DropForeignKey(
                name: "FK_MailAttachmentes_Mails_MailId",
                table: "MailAttachmentes");

            migrationBuilder.DropPrimaryKey(
                name: "PK_MailAttachmentes",
                table: "MailAttachmentes");

            migrationBuilder.DropPrimaryKey(
                name: "PK_MailAddresses",
                table: "MailAddresses");

            migrationBuilder.RenameTable(
                name: "MailAttachmentes",
                newName: "GameMailAttachment");

            migrationBuilder.RenameTable(
                name: "MailAddresses",
                newName: "MailAddress");

            migrationBuilder.RenameIndex(
                name: "IX_MailAttachmentes_MailId",
                table: "GameMailAttachment",
                newName: "IX_GameMailAttachment_MailId");

            migrationBuilder.RenameIndex(
                name: "IX_MailAddresses_ThingId",
                table: "MailAddress",
                newName: "IX_MailAddress_ThingId");

            migrationBuilder.RenameIndex(
                name: "IX_MailAddresses_MailId",
                table: "MailAddress",
                newName: "IX_MailAddress_MailId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_GameMailAttachment",
                table: "GameMailAttachment",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_MailAddress",
                table: "MailAddress",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_GameMailAttachment_Mails_MailId",
                table: "GameMailAttachment",
                column: "MailId",
                principalTable: "Mails",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_MailAddress_Mails_MailId",
                table: "MailAddress",
                column: "MailId",
                principalTable: "Mails",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
