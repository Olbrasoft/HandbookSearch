using System.CommandLine;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Olbrasoft.HandbookSearch.Business;
using Olbrasoft.HandbookSearch.Business.Services;
using Olbrasoft.HandbookSearch.Data.EntityFrameworkCore;

var rootCommand = new RootCommand("HandbookSearch CLI - Import markdown documents into database");

// import-all command
var importAllCommand = new Command("import-all", "Import all markdown files from handbook directory");
var pathOption = new Option<string>(
    name: "--path",
    description: "Path to the engineering handbook directory")
{
    IsRequired = true
};
var languageOptionAll = new Option<string>(
    name: "--language",
    description: "Language code (e.g., 'en', 'cs')",
    getDefaultValue: () => "en");
importAllCommand.AddOption(pathOption);
importAllCommand.AddOption(languageOptionAll);

importAllCommand.SetHandler(async (string path, string language) =>
{
    var host = CreateHostBuilder().Build();
    var importService = host.Services.GetRequiredService<IDocumentImportService>();
    var logger = host.Services.GetRequiredService<ILogger<Program>>();

    logger.LogInformation("Starting import from: {Path} (Language: {Language})", path, language);

    try
    {
        var result = await importService.ImportAllAsync(path, language);

        Console.WriteLine("\n✅ Import completed!");
        Console.WriteLine($"   Added:   {result.Added}");
        Console.WriteLine($"   Updated: {result.Updated}");
        Console.WriteLine($"   Skipped: {result.Skipped}");
        Console.WriteLine($"   Total:   {result.Total}");

        if (result.HasErrors)
        {
            Console.WriteLine($"\n⚠️  Errors: {result.Errors.Count}");
            foreach (var error in result.Errors)
            {
                Console.WriteLine($"   - {error}");
            }
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Import failed");
        Console.WriteLine($"\n❌ Error: {ex.Message}");
        Environment.Exit(1);
    }
}, pathOption, languageOptionAll);

// import-files command
var importFilesCommand = new Command("import-files", "Import specific markdown files");
var filesOption = new Option<string>(
    name: "--files",
    description: "Comma-separated list of file paths")
{
    IsRequired = true
};
var languageOptionFiles = new Option<string>(
    name: "--language",
    description: "Language code (e.g., 'en', 'cs')",
    getDefaultValue: () => "en");
importFilesCommand.AddOption(filesOption);
importFilesCommand.AddOption(languageOptionFiles);

importFilesCommand.SetHandler(async (string files, string language) =>
{
    var host = CreateHostBuilder().Build();
    var importService = host.Services.GetRequiredService<IDocumentImportService>();
    var logger = host.Services.GetRequiredService<ILogger<Program>>();

    var filePaths = files.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    logger.LogInformation("Starting import of {Count} files (Language: {Language})", filePaths.Length, language);

    var imported = 0;
    var skipped = 0;
    var errors = new List<string>();

    foreach (var filePath in filePaths)
    {
        try
        {
            var result = await importService.ImportFileAsync(filePath, language, null);
            if (result)
            {
                imported++;
                Console.WriteLine($"✓ {filePath}");
            }
            else
            {
                skipped++;
                Console.WriteLine($"○ {filePath} (skipped - no changes)");
            }
        }
        catch (Exception ex)
        {
            errors.Add($"{filePath}: {ex.Message}");
            Console.WriteLine($"✗ {filePath}: {ex.Message}");
        }
    }

    Console.WriteLine($"\n✅ Import completed!");
    Console.WriteLine($"   Imported: {imported}");
    Console.WriteLine($"   Skipped:  {skipped}");
    Console.WriteLine($"   Total:    {filePaths.Length}");

    if (errors.Count > 0)
    {
        Console.WriteLine($"\n⚠️  Errors: {errors.Count}");
        Environment.Exit(1);
    }
}, filesOption, languageOptionFiles);

rootCommand.AddCommand(importAllCommand);
rootCommand.AddCommand(importFilesCommand);

return await rootCommand.InvokeAsync(args);

static IHostBuilder CreateHostBuilder() =>
    Host.CreateDefaultBuilder()
        .ConfigureAppConfiguration((context, config) =>
        {
            config.AddJsonFile("appsettings.json", optional: false);
            config.AddEnvironmentVariables();
        })
        .ConfigureServices((context, services) =>
        {
            // Configuration
            services.Configure<OllamaOptions>(context.Configuration.GetSection("Ollama"));

            // Database
            var connectionString = context.Configuration.GetConnectionString("DefaultConnection");
            services.AddDbContext<HandbookSearchDbContext>(options =>
                options.UseNpgsql(connectionString, o => o.UseVector()));

            // HTTP Client for EmbeddingService
            services.AddHttpClient<IEmbeddingService, EmbeddingService>();

            // Business Services
            services.AddScoped<IDocumentImportService, DocumentImportService>();
            services.AddScoped<ISearchService, SearchService>();
        });
