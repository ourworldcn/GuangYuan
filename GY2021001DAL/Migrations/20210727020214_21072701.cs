using Microsoft.EntityFrameworkCore.Migrations;

namespace GY2021001DAL.Migrations
{
    public partial class _21072701 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Body",
                table: "GameThingBase",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "From",
                table: "GameThingBase",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Subject",
                table: "GameThingBase",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "To",
                table: "GameThingBase",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Body",
                table: "GameThingBase");

            migrationBuilder.DropColumn(
                name: "From",
                table: "GameThingBase");

            migrationBuilder.DropColumn(
                name: "Subject",
                table: "GameThingBase");

            migrationBuilder.DropColumn(
                name: "To",
                table: "GameThingBase");
        }
    }
}
