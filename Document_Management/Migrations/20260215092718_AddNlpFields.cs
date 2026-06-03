using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DocumentManagementApp.Migrations
{
    /// <inheritdoc />
    public partial class AddNlpFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsNlpProcessed",
                table: "Documents",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "NlpStatus",
                table: "Documents",
                type: "TEXT",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ProcessedText",
                table: "Documents",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsNlpProcessed",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "NlpStatus",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "ProcessedText",
                table: "Documents");
        }
    }
}
