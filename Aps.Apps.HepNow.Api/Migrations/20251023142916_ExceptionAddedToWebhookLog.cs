using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Aps.Apps.HepNow.Migrations
{
    public partial class ExceptionAddedToWebhookLog : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Exception",
                table: "WebhookLog",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Exception",
                table: "WebhookLog");
        }
    }
}
