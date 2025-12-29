using System.CommandLine;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Olbrasoft.HandbookSearch.Translation.Cli.Configuration;
using Olbrasoft.HandbookSearch.Translation.Cli.Services;

var rootCommand = new RootCommand("HandbookSearch Translation CLI - Translate engineering handbook to target language");

// translate-all command
var translateAllCommand = new Command("translate-all", "Translate all markdown files to target language");

var sourcePathOption = new Option<string>(
    name: "--source",
    description: "Source handbook directory (e.g., ~/GitHub/Olbrasoft/engineering-handbook)")
{
    IsRequired = true
};

var targetPathOption = new Option<string>(
    name: "--target",
    description: "Target directory for translated files (e.g., ~/GitHub/Olbrasoft/engineering-handbook-cs)")
{
    IsRequired = true
};

var targetLangOption = new Option<string>(
    name: "--target-lang",
    description: "Target language code",
    getDefaultValue: () => "cs");

translateAllCommand.AddOption(sourcePathOption);
translateAllCommand.AddOption(targetPathOption);
translateAllCommand.AddOption(targetLangOption);

translateAllCommand.SetHandler(async (string sourcePath, string targetPath, string targetLang) =>
{
    var host = CreateHostBuilder(args).Build();
    var translator = host.Services.GetRequiredService<IAzureTranslatorService>();
    var logger = host.Services.GetRequiredService<ILogger<Program>>();

    logger.LogInformation("Starting translation from {Source} to {Target}", sourcePath, targetPath);
    logger.LogInformation("Target language: {TargetLang}", targetLang);

    // Validate source path
    if (!Directory.Exists(sourcePath))
    {
        Console.WriteLine($"❌ Error: Source directory not found: {sourcePath}");
        Environment.Exit(1);
    }

    // Validate source path is not a system directory
    var fullSourcePath = Path.GetFullPath(sourcePath);
    if (fullSourcePath.StartsWith("/etc/", StringComparison.OrdinalIgnoreCase) ||
        fullSourcePath.StartsWith("/sys/", StringComparison.OrdinalIgnoreCase) ||
        fullSourcePath.StartsWith("/proc/", StringComparison.OrdinalIgnoreCase))
    {
        Console.WriteLine($"❌ Error: Cannot translate from system directory: {sourcePath}");
        Environment.Exit(1);
    }

    // Validate and create target directory
    try
    {
        var fullTargetPath = Path.GetFullPath(targetPath);

        // Ensure target is not a system directory
        if (fullTargetPath.StartsWith("/etc/", StringComparison.OrdinalIgnoreCase) ||
            fullTargetPath.StartsWith("/sys/", StringComparison.OrdinalIgnoreCase) ||
            fullTargetPath.StartsWith("/proc/", StringComparison.OrdinalIgnoreCase))
        {
            Console.WriteLine($"❌ Error: Cannot write to system directory: {targetPath}");
            Environment.Exit(1);
        }

        Directory.CreateDirectory(targetPath);
    }
    catch (UnauthorizedAccessException)
    {
        Console.WriteLine($"❌ Error: Permission denied writing to: {targetPath}");
        Environment.Exit(1);
    }
    catch (IOException ex)
    {
        Console.WriteLine($"❌ Error: Cannot create target directory: {ex.Message}");
        Environment.Exit(1);
    }

    // Get all markdown files
    var markdownFiles = Directory.GetFiles(sourcePath, "*.md", SearchOption.AllDirectories);

    Console.WriteLine($"\n📄 Found {markdownFiles.Length} markdown files");
    Console.WriteLine($"⏱️  Estimated time: {EstimateTime(markdownFiles, sourcePath)}");
    Console.WriteLine("\n🔄 Starting translation...\n");

    var translated = 0;
    var errors = new List<string>();

    foreach (var sourceFile in markdownFiles)
    {
        try
        {
            // Calculate relative path
            var relativePath = Path.GetRelativePath(sourcePath, sourceFile);
            var targetFile = Path.Combine(targetPath, relativePath);

            // Create target directory
            var targetDir = Path.GetDirectoryName(targetFile);
            if (!string.IsNullOrEmpty(targetDir))
            {
                Directory.CreateDirectory(targetDir);
            }

            // Read source content
            var content = await File.ReadAllTextAsync(sourceFile);

            Console.Write($"📝 {relativePath}... ");

            // Translate
            var translatedContent = await translator.TranslateAsync(content, targetLang);

            // Add metadata marker
            var markerContent = $"<!-- AI_AGENTS_IGNORE: This is a translation to '{targetLang}' for embedding search only. Agents should use the English version. -->\n\n{translatedContent}";

            // Write to target file
            await File.WriteAllTextAsync(targetFile, markerContent);

            translated++;
            Console.WriteLine("✅");
        }
        catch (HttpRequestException ex) when (ex.Message.Contains("401") || ex.Message.Contains("403") || ex.Message.Contains("429"))
        {
            // Critical errors - stop processing
            Console.WriteLine($"\n❌ Critical error: {ex.Message}");
            logger.LogError(ex, "Critical translation error - stopping execution");

            if (ex.Message.Contains("401") || ex.Message.Contains("403"))
            {
                Console.WriteLine("⚠️  Authentication failed. Please check your API key and region configuration.");
            }
            else if (ex.Message.Contains("429"))
            {
                Console.WriteLine("⚠️  Rate limit exceeded or quota exhausted. Please try again later.");
            }

            Environment.Exit(1);
        }
        catch (Exception ex)
        {
            errors.Add($"{Path.GetFileName(sourceFile)}: {ex.Message}");
            Console.WriteLine($"❌ {ex.Message}");
            logger.LogError(ex, "Translation failed for {File}", sourceFile);
        }
    }

    Console.WriteLine($"\n✅ Translation completed!");
    Console.WriteLine($"   Translated: {translated}");
    Console.WriteLine($"   Errors:     {errors.Count}");
    Console.WriteLine($"   Total:      {markdownFiles.Length}");

    if (errors.Count > 0)
    {
        Console.WriteLine($"\n⚠️  Errors:");
        foreach (var error in errors)
        {
            Console.WriteLine($"   - {error}");
        }
        Environment.Exit(1);
    }

}, sourcePathOption, targetPathOption, targetLangOption);

rootCommand.AddCommand(translateAllCommand);

return await rootCommand.InvokeAsync(args);

static IHostBuilder CreateHostBuilder(string[] args) =>
    Host.CreateDefaultBuilder(args)
        .ConfigureAppConfiguration((context, config) =>
        {
            config.AddJsonFile("appsettings.json", optional: false);
            config.AddUserSecrets<Program>(optional: true);
            config.AddEnvironmentVariables();
        })
        .ConfigureServices((context, services) =>
        {
            // Configuration
            services.Configure<AzureTranslatorOptions>(
                context.Configuration.GetSection(AzureTranslatorOptions.SectionName));

            // Configuration validation
            services.AddSingleton<IValidateOptions<AzureTranslatorOptions>, AzureTranslatorOptionsValidator>();

            // HTTP Client
            services.AddHttpClient<IAzureTranslatorService, AzureTranslatorService>();
        });

static string EstimateTime(string[] files, string sourcePath)
{
    var totalChars = 0L;
    foreach (var file in files)
    {
        try
        {
            var content = File.ReadAllText(file);
            totalChars += content.Length;
        }
        catch
        {
            // Skip files that can't be read
        }
    }

    // Rough estimate: 555 chars/second + 20% safety margin
    var seconds = (totalChars / 555.0) * 1.2;
    var minutes = (int)Math.Ceiling(seconds / 60);

    return $"{minutes} minutes (approximately)";
}
