using System.CommandLine;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Olbrasoft.HandbookSearch.Business;
using Olbrasoft.HandbookSearch.Business.Configuration;
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
var translateCsOption = new Option<bool>(
    name: "--translate-cs",
    description: "Generate Czech translation in-memory and create Czech embedding (translation not saved to disk)",
    getDefaultValue: () => false);
var handbookPathOptionFiles = new Option<string?>(
    name: "--handbook-path",
    description: "Root path of the handbook directory (for calculating relative paths)");
importFilesCommand.AddOption(filesOption);
importFilesCommand.AddOption(languageOptionFiles);
importFilesCommand.AddOption(translateCsOption);
importFilesCommand.AddOption(handbookPathOptionFiles);

importFilesCommand.SetHandler(async (string files, string language, bool translateCs, string? handbookPath) =>
{
    var host = CreateHostBuilder().Build();
    var importService = host.Services.GetRequiredService<IDocumentImportService>();
    var logger = host.Services.GetRequiredService<ILogger<Program>>();

    var filePaths = files.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    logger.LogInformation("Starting import of {Count} files (Language: {Language}, TranslateCs: {TranslateCs})", filePaths.Length, language, translateCs);

    var imported = 0;
    var skipped = 0;
    var errors = new List<string>();

    foreach (var filePath in filePaths)
    {
        try
        {
            var result = await importService.ImportFileAsync(filePath, language, handbookPath, translateCs);
            if (result)
            {
                imported++;
                var suffix = translateCs ? " (+Czech embedding)" : "";
                Console.WriteLine($"✓ {filePath}{suffix}");
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
}, filesOption, languageOptionFiles, translateCsOption, handbookPathOptionFiles);

// delete-files command
var deleteFilesCommand = new Command("delete-files", "Delete specific documents from database");
var deleteFilesOption = new Option<string>(
    name: "--files",
    description: "Comma-separated list of relative file paths (e.g., 'docs/guide.md,development-guidelines/workflow-guide.md')")
{
    IsRequired = true
};
deleteFilesCommand.AddOption(deleteFilesOption);

deleteFilesCommand.SetHandler(async (string files) =>
{
    var host = CreateHostBuilder().Build();
    var importService = host.Services.GetRequiredService<IDocumentImportService>();
    var logger = host.Services.GetRequiredService<ILogger<Program>>();

    var relativePaths = files.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    logger.LogInformation("Starting deletion of {Count} documents", relativePaths.Length);

    var deleted = 0;
    var notFound = 0;

    foreach (var relativePath in relativePaths)
    {
        try
        {
            var result = await importService.DeleteDocumentAsync(relativePath);
            if (result)
            {
                deleted++;
                Console.WriteLine($"✓ {relativePath} (deleted)");
            }
            else
            {
                notFound++;
                Console.WriteLine($"○ {relativePath} (not found)");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deleting {Path}", relativePath);
            Console.WriteLine($"✗ {relativePath}: {ex.Message}");
        }
    }

    Console.WriteLine($"\n✅ Deletion completed!");
    Console.WriteLine($"   Deleted:   {deleted}");
    Console.WriteLine($"   Not found: {notFound}");
    Console.WriteLine($"   Total:     {relativePaths.Length}");
}, deleteFilesOption);

rootCommand.AddCommand(importAllCommand);
rootCommand.AddCommand(importFilesCommand);
rootCommand.AddCommand(deleteFilesCommand);

return await rootCommand.InvokeAsync(args);

static IHostBuilder CreateHostBuilder() =>
    Host.CreateDefaultBuilder()
        .ConfigureAppConfiguration((context, config) =>
        {
            config.AddJsonFile("appsettings.json", optional: false);
            config.AddSecureStore(); // Add SecureStore for encrypted secrets
            config.AddEnvironmentVariables();
        })
        .ConfigureServices((context, services) =>
        {
            // Configuration
            services.Configure<OllamaOptions>(context.Configuration.GetSection("Ollama"));
            services.Configure<AzureTranslatorOptions>(context.Configuration.GetSection("AzureTranslator"));

            // Database - build connection string from parts (password from SecureStore)
            var connectionString = BuildConnectionString(context.Configuration.GetSection("Database"));
            services.AddDbContext<HandbookSearchDbContext>(options =>
                options.UseNpgsql(connectionString, o => o.UseVector()));

            // HTTP Clients
            services.AddHttpClient<IEmbeddingService, EmbeddingService>();
            services.AddHttpClient<ITranslationService, AzureTranslationService>();

            // Business Services
            services.AddScoped<IDocumentImportService, DocumentImportService>();
            services.AddScoped<ISearchService, SearchService>();
        });

/// <summary>
/// Builds a PostgreSQL connection string from configuration section.
/// Password is expected from SecureStore (Database:Password key).
/// </summary>
static string BuildConnectionString(IConfigurationSection dbConfig)
{
    var host = dbConfig["Host"] ?? "localhost";
    var database = dbConfig["Name"] ?? "handbook_search";
    var username = dbConfig["Username"] ?? "postgres";
    var password = dbConfig["Password"]; // From SecureStore

    var connStr = $"Host={host};Database={database};Username={username}";
    if (!string.IsNullOrEmpty(password))
    {
        connStr += $";Password={password}";
    }
    return connStr;
}
