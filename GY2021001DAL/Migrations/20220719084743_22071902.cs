using Microsoft.EntityFrameworkCore.Migrations;

namespace GuangYuan.GY001.UserDb.Migrations
{
    public partial class _22071902 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "JsonObjectString",
                table: "WarNewspaper",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "JsonObjectString",
                table: "SeparateThings",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "JsonObjectString",
                table: "Mails",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "JsonObjectString",
                table: "MailAttachmentes",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "JsonObjectString",
                table: "MailAddresses",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "JsonObjectString",
                table: "Guild",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "JsonObjectString",
                table: "GameUsers",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "JsonObjectString",
                table: "GameItems",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "JsonObjectString",
                table: "GameChars",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "JsonObjectString",
                table: "GameBooty",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "JsonObjectString",
                table: "WarNewspaper");

            migrationBuilder.DropColumn(
                name: "JsonObjectString",
                table: "SeparateThings");

            migrationBuilder.DropColumn(
                name: "JsonObjectString",
                table: "Mails");

            migrationBuilder.DropColumn(
                name: "JsonObjectString",
                table: "MailAttachmentes");

            migrationBuilder.DropColumn(
                name: "JsonObjectString",
                table: "MailAddresses");

            migrationBuilder.DropColumn(
                name: "JsonObjectString",
                table: "Guild");

            migrationBuilder.DropColumn(
                name: "JsonObjectString",
                table: "GameUsers");

            migrationBuilder.DropColumn(
                name: "JsonObjectString",
                table: "GameItems");

            migrationBuilder.DropColumn(
                name: "JsonObjectString",
                table: "GameChars");

            migrationBuilder.DropColumn(
                name: "JsonObjectString",
                table: "GameBooty");
        }
    }
}
