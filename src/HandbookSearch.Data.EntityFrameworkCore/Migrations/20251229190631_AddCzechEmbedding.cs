using Microsoft.EntityFrameworkCore.Migrations;
using Pgvector;

#nullable disable

namespace Olbrasoft.HandbookSearch.Data.EntityFrameworkCore.Migrations
{
    /// <inheritdoc />
    public partial class AddCzechEmbedding : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Vector>(
                name: "EmbeddingCs",
                table: "Documents",
                type: "vector(768)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Documents_EmbeddingCs",
                table: "Documents",
                column: "EmbeddingCs")
                .Annotation("Npgsql:IndexMethod", "hnsw")
                .Annotation("Npgsql:IndexOperators", new[] { "vector_cosine_ops" })
                .Annotation("Npgsql:StorageParameter:ef_construction", 64)
                .Annotation("Npgsql:StorageParameter:m", 16);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Documents_EmbeddingCs",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "EmbeddingCs",
                table: "Documents");
        }
    }
}
