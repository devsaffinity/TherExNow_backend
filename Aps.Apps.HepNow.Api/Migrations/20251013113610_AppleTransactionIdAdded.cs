using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Aps.Apps.HepNow.Migrations
{
    public partial class AppleTransactionIdAdded : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AppleTransactionId",
                table: "Memberships",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AppleTransactionId",
                table: "Memberships");
        }
    }
}
