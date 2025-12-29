using Microsoft.EntityFrameworkCore;
using Olbrasoft.HandbookSearch.Data.Entities;

namespace Olbrasoft.HandbookSearch.Data.EntityFrameworkCore;

public class HandbookSearchDbContext : DbContext
{
    public DbSet<Document> Documents => Set<Document>();

    public HandbookSearchDbContext(DbContextOptions<HandbookSearchDbContext> options)
        : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Check if using PostgreSQL provider
        var isPostgreSQL = Database.IsNpgsql();

        if (isPostgreSQL)
        {
            // Enable pgvector extension (PostgreSQL only)
            modelBuilder.HasPostgresExtension("vector");
        }

        modelBuilder.Entity<Document>(entity =>
        {
            // Use snake_case table name for PostgreSQL
            entity.ToTable("documents");

            entity.HasKey(e => e.Id);

            // Unique constraint on file_path (one record per English file)
            entity.HasIndex(e => e.FilePath)
                .IsUnique()
                .HasDatabaseName("idx_documents_file_path");

            if (isPostgreSQL)
            {
                // HNSW index for English embeddings (PostgreSQL only)
                entity.HasIndex(e => e.Embedding)
                    .HasMethod("hnsw")
                    .HasOperators("vector_cosine_ops")
                    .HasStorageParameter("m", 16)
                    .HasStorageParameter("ef_construction", 64)
                    .HasDatabaseName("idx_documents_embedding");

                // HNSW index for Czech embeddings (PostgreSQL only)
                entity.HasIndex(e => e.EmbeddingCs)
                    .HasMethod("hnsw")
                    .HasOperators("vector_cosine_ops")
                    .HasStorageParameter("m", 16)
                    .HasStorageParameter("ef_construction", 64)
                    .HasDatabaseName("idx_documents_embedding_cs");

                // Timestamps with PostgreSQL default
                entity.Property(e => e.CreatedAt)
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.Property(e => e.UpdatedAt)
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");
            }
            else
            {
                // For InMemory and other providers, use .NET defaults
                entity.Property(e => e.CreatedAt)
                    .HasDefaultValueSql("GETDATE()");
                entity.Property(e => e.UpdatedAt)
                    .HasDefaultValueSql("GETDATE()");
            }
        });
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            // UseVector() is REQUIRED for pgvector support!
            optionsBuilder.UseNpgsql("Name=ConnectionStrings:DefaultConnection",
                o => o.UseVector());
        }
    }
}
