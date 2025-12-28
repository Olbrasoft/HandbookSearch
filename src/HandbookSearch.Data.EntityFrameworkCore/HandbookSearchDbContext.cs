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
        // Enable pgvector extension
        modelBuilder.HasPostgresExtension("vector");

        modelBuilder.Entity<Document>(entity =>
        {
            entity.HasKey(e => e.Id);

            // Unique constraint on file path
            entity.HasIndex(e => e.FilePath).IsUnique();

            // HNSW index for cosine similarity search
            entity.HasIndex(e => e.Embedding)
                .HasMethod("hnsw")
                .HasOperators("vector_cosine_ops")
                .HasStorageParameter("m", 16)
                .HasStorageParameter("ef_construction", 64);

            // Timestamps
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");
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
