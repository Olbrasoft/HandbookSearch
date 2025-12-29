using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Olbrasoft.HandbookSearch.Data.EntityFrameworkCore.Migrations
{
    /// <inheritdoc />
    public partial class AddLanguageColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Documents_FilePath",
                table: "Documents");

            migrationBuilder.AddColumn<string>(
                name: "Language",
                table: "Documents",
                type: "text",
                nullable: false,
                defaultValue: "en");

            migrationBuilder.CreateIndex(
                name: "IX_Documents_FilePath_Language",
                table: "Documents",
                columns: new[] { "FilePath", "Language" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Documents_Language",
                table: "Documents",
                column: "Language");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Documents_FilePath_Language",
                table: "Documents");

            migrationBuilder.DropIndex(
                name: "IX_Documents_Language",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "Language",
                table: "Documents");

            migrationBuilder.CreateIndex(
                name: "IX_Documents_FilePath",
                table: "Documents",
                column: "FilePath",
                unique: true);
        }
    }
}
