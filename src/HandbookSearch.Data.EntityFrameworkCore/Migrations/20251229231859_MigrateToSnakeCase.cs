using Microsoft.EntityFrameworkCore.Migrations;
using Pgvector;

#nullable disable

namespace Olbrasoft.HandbookSearch.Data.EntityFrameworkCore.Migrations
{
    /// <inheritdoc />
    public partial class MigrateToSnakeCase : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_Documents",
                table: "Documents");

            migrationBuilder.RenameTable(
                name: "Documents",
                newName: "documents");

            migrationBuilder.RenameColumn(
                name: "Title",
                table: "documents",
                newName: "title");

            migrationBuilder.RenameColumn(
                name: "Embedding",
                table: "documents",
                newName: "embedding");

            migrationBuilder.RenameColumn(
                name: "Content",
                table: "documents",
                newName: "content");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "documents",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "UpdatedAt",
                table: "documents",
                newName: "updated_at");

            migrationBuilder.RenameColumn(
                name: "FilePath",
                table: "documents",
                newName: "file_path");

            migrationBuilder.RenameColumn(
                name: "EmbeddingCs",
                table: "documents",
                newName: "embedding_cs");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "documents",
                newName: "created_at");

            migrationBuilder.RenameColumn(
                name: "ContentHash",
                table: "documents",
                newName: "content_hash");

            migrationBuilder.RenameIndex(
                name: "IX_Documents_FilePath",
                table: "documents",
                newName: "idx_documents_file_path");

            migrationBuilder.RenameIndex(
                name: "IX_Documents_EmbeddingCs",
                table: "documents",
                newName: "idx_documents_embedding_cs");

            migrationBuilder.RenameIndex(
                name: "IX_Documents_Embedding",
                table: "documents",
                newName: "idx_documents_embedding");

            migrationBuilder.AlterColumn<Vector>(
                name: "embedding",
                table: "documents",
                type: "vector(1024)",
                nullable: true,
                oldClrType: typeof(Vector),
                oldType: "vector(768)",
                oldNullable: true);

            migrationBuilder.AlterColumn<Vector>(
                name: "embedding_cs",
                table: "documents",
                type: "vector(1024)",
                nullable: true,
                oldClrType: typeof(Vector),
                oldType: "vector(768)",
                oldNullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_documents",
                table: "documents",
                column: "id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_documents",
                table: "documents");

            migrationBuilder.RenameTable(
                name: "documents",
                newName: "Documents");

            migrationBuilder.RenameColumn(
                name: "title",
                table: "Documents",
                newName: "Title");

            migrationBuilder.RenameColumn(
                name: "embedding",
                table: "Documents",
                newName: "Embedding");

            migrationBuilder.RenameColumn(
                name: "content",
                table: "Documents",
                newName: "Content");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "Documents",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "updated_at",
                table: "Documents",
                newName: "UpdatedAt");

            migrationBuilder.RenameColumn(
                name: "file_path",
                table: "Documents",
                newName: "FilePath");

            migrationBuilder.RenameColumn(
                name: "embedding_cs",
                table: "Documents",
                newName: "EmbeddingCs");

            migrationBuilder.RenameColumn(
                name: "created_at",
                table: "Documents",
                newName: "CreatedAt");

            migrationBuilder.RenameColumn(
                name: "content_hash",
                table: "Documents",
                newName: "ContentHash");

            migrationBuilder.RenameIndex(
                name: "idx_documents_file_path",
                table: "Documents",
                newName: "IX_Documents_FilePath");

            migrationBuilder.RenameIndex(
                name: "idx_documents_embedding_cs",
                table: "Documents",
                newName: "IX_Documents_EmbeddingCs");

            migrationBuilder.RenameIndex(
                name: "idx_documents_embedding",
                table: "Documents",
                newName: "IX_Documents_Embedding");

            migrationBuilder.AlterColumn<Vector>(
                name: "Embedding",
                table: "Documents",
                type: "vector(768)",
                nullable: true,
                oldClrType: typeof(Vector),
                oldType: "vector(1024)",
                oldNullable: true);

            migrationBuilder.AlterColumn<Vector>(
                name: "EmbeddingCs",
                table: "Documents",
                type: "vector(768)",
                nullable: true,
                oldClrType: typeof(Vector),
                oldType: "vector(1024)",
                oldNullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_Documents",
                table: "Documents",
                column: "Id");
        }
    }
}
