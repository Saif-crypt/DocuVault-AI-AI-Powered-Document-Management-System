using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DocumentManagementApp.Migrations
{
    /// <inheritdoc />
    public partial class AddNlpFieldsToDocument : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ProcessedText",
                table: "Documents",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NlpStatus",
                table: "Documents",
                type: "TEXT",
                maxLength: 20,
                nullable: false,
                defaultValue: "Pending");

            migrationBuilder.AddColumn<int>(
                name: "IsNlpProcessed",
                table: "Documents",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ProcessedText",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "NlpStatus",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "IsNlpProcessed",
                table: "Documents");
        }
    }
}
