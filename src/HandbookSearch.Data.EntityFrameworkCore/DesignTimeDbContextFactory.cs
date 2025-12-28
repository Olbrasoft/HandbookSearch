using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Olbrasoft.HandbookSearch.Data.EntityFrameworkCore;

public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<HandbookSearchDbContext>
{
    public HandbookSearchDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<HandbookSearchDbContext>();

        // Design-time connection string (no password for local development)
        optionsBuilder.UseNpgsql(
            "Host=localhost;Database=handbook_search;Username=postgres",
            o => o.UseVector());

        return new HandbookSearchDbContext(optionsBuilder.Options);
    }
}
