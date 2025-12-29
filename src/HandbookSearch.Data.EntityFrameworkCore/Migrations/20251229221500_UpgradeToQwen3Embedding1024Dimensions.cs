using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Olbrasoft.HandbookSearch.Data.EntityFrameworkCore.Migrations
{
    /// <inheritdoc />
    public partial class UpgradeToQwen3Embedding1024Dimensions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Drop existing HNSW indexes (cannot alter column type with index)
            migrationBuilder.Sql("DROP INDEX IF EXISTS \"IX_Documents_Embedding\";");
            migrationBuilder.Sql("DROP INDEX IF EXISTS \"IX_Documents_EmbeddingCs\";");

            // Clear existing embeddings (incompatible dimensions 768 vs 1024)
            migrationBuilder.Sql("UPDATE \"Documents\" SET \"Embedding\" = NULL, \"EmbeddingCs\" = NULL;");

            // Change column types from vector(768) to vector(1024)
            migrationBuilder.Sql("ALTER TABLE \"Documents\" ALTER COLUMN \"Embedding\" TYPE vector(1024);");
            migrationBuilder.Sql("ALTER TABLE \"Documents\" ALTER COLUMN \"EmbeddingCs\" TYPE vector(1024);");

            // Recreate HNSW indexes with new dimensions
            migrationBuilder.Sql(@"
                CREATE INDEX ""IX_Documents_Embedding""
                ON ""Documents""
                USING hnsw (""Embedding"" vector_cosine_ops)
                WITH (m = 16, ef_construction = 64);
            ");

            migrationBuilder.Sql(@"
                CREATE INDEX ""IX_Documents_EmbeddingCs""
                ON ""Documents""
                USING hnsw (""EmbeddingCs"" vector_cosine_ops)
                WITH (m = 16, ef_construction = 64);
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop 1024-dim HNSW indexes
            migrationBuilder.Sql("DROP INDEX IF EXISTS \"IX_Documents_Embedding\";");
            migrationBuilder.Sql("DROP INDEX IF EXISTS \"IX_Documents_EmbeddingCs\";");

            // Clear embeddings
            migrationBuilder.Sql("UPDATE \"Documents\" SET \"Embedding\" = NULL, \"EmbeddingCs\" = NULL;");

            // Change column types back to vector(768)
            migrationBuilder.Sql("ALTER TABLE \"Documents\" ALTER COLUMN \"Embedding\" TYPE vector(768);");
            migrationBuilder.Sql("ALTER TABLE \"Documents\" ALTER COLUMN \"EmbeddingCs\" TYPE vector(768);");

            // Recreate HNSW indexes with 768 dimensions
            migrationBuilder.Sql(@"
                CREATE INDEX ""IX_Documents_Embedding""
                ON ""Documents""
                USING hnsw (""Embedding"" vector_cosine_ops)
                WITH (m = 16, ef_construction = 64);
            ");

            migrationBuilder.Sql(@"
                CREATE INDEX ""IX_Documents_EmbeddingCs""
                ON ""Documents""
                USING hnsw (""EmbeddingCs"" vector_cosine_ops)
                WITH (m = 16, ef_construction = 64);
            ");
        }
    }
}
