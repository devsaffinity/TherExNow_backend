using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Aps.Apps.HepNow.Migrations
{
    public partial class IsMonthlyAddedToMembership : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsMonthly",
                table: "Memberships",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsMonthly",
                table: "Memberships");
        }
    }
}
