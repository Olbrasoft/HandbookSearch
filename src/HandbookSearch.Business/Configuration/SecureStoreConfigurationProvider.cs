using Microsoft.Extensions.Configuration;
using NeoSmart.SecureStore;

namespace Olbrasoft.HandbookSearch.Business.Configuration;

/// <summary>
/// Configuration provider that loads secrets from SecureStore encrypted vault.
/// Secrets are loaded into IConfiguration and can be accessed by key name.
/// </summary>
/// <remarks>
/// Note: Logging is not available during configuration provider load phase.
/// Console output is used for diagnostics instead.
/// </remarks>
public class SecureStoreConfigurationProvider : ConfigurationProvider
{
    private readonly string _secretsPath;
    private readonly string _keyPath;

    /// <summary>
    /// Initializes a new instance of SecureStoreConfigurationProvider.
    /// </summary>
    /// <param name="secretsPath">Path to secrets.json vault file.</param>
    /// <param name="keyPath">Path to secrets.key file.</param>
    public SecureStoreConfigurationProvider(string secretsPath, string keyPath)
    {
        _secretsPath = secretsPath;
        _keyPath = keyPath;
    }

    /// <summary>
    /// Loads secrets from SecureStore vault into configuration.
    /// </summary>
    public override void Load()
    {
        if (!File.Exists(_secretsPath))
        {
            Console.WriteLine($"[SecureStore] Vault not found at {_secretsPath}, skipping");
            return;
        }

        if (!File.Exists(_keyPath))
        {
            Console.WriteLine($"[SecureStore] Key file not found at {_keyPath}, skipping");
            return;
        }

        try
        {
            using var secrets = SecretsManager.LoadStore(_secretsPath);
            secrets.LoadKeyFromFile(_keyPath);

            var loadedCount = 0;
            foreach (var key in secrets.Keys)
            {
                Data[key] = secrets.Get(key);
                loadedCount++;
            }

            Console.WriteLine($"[SecureStore] Loaded {loadedCount} secrets from {_secretsPath}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SecureStore] ERROR: Failed to load vault from {_secretsPath}: {ex.Message}");
            // Don't throw - allow application to start without secrets (will fail later when secret is needed)
        }
    }
}

/// <summary>
/// Configuration source for SecureStore provider.
/// </summary>
public class SecureStoreConfigurationSource : IConfigurationSource
{
    /// <summary>
    /// Gets or sets the path to secrets.json vault file.
    /// </summary>
    public string SecretsPath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the path to secrets.key file.
    /// </summary>
    public string KeyPath { get; set; } = string.Empty;

    /// <inheritdoc />
    public IConfigurationProvider Build(IConfigurationBuilder builder)
    {
        return new SecureStoreConfigurationProvider(SecretsPath, KeyPath);
    }
}

/// <summary>
/// Extension methods for adding SecureStore configuration source.
/// </summary>
public static class SecureStoreConfigurationExtensions
{
    /// <summary>
    /// Default path to the SecureStore secrets vault file (encrypted secrets.json).
    /// The vault is stored under a dedicated <c>secrets/</c> directory and is kept
    /// separate from the encryption key directory to avoid co-locating encrypted data
    /// and decryption material in the same path.
    /// </summary>
    public const string DefaultSecretsPath = "~/.config/handbook-search/secrets/secrets.json";

    /// <summary>
    /// Default path to the encryption key file used to decrypt the SecureStore vault.
    /// The key is stored under a separate <c>keys/</c> directory so that filesystem
    /// permissions, backup policies, and operational handling can differ from the
    /// encrypted vault itself, following the principle of separation of concerns.
    /// </summary>
    public const string DefaultKeyPath = "~/.config/handbook-search/keys/secrets.key";

    /// <summary>
    /// Adds SecureStore vault as a configuration source using default paths.
    /// Secrets from the vault will be available via IConfiguration.
    /// </summary>
    /// <param name="builder">Configuration builder.</param>
    /// <returns>The configuration builder for chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when builder is null.</exception>
    public static IConfigurationBuilder AddSecureStore(this IConfigurationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        return builder.AddSecureStore(DefaultSecretsPath, DefaultKeyPath);
    }

    /// <summary>
    /// Adds SecureStore vault as a configuration source.
    /// Secrets from the vault will be available via IConfiguration.
    /// </summary>
    /// <param name="builder">Configuration builder.</param>
    /// <param name="secretsPath">Path to secrets.json vault file.</param>
    /// <param name="keyPath">Path to secrets.key file.</param>
    /// <returns>The configuration builder for chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when builder is null.</exception>
    public static IConfigurationBuilder AddSecureStore(
        this IConfigurationBuilder builder,
        string secretsPath,
        string keyPath)
    {
        ArgumentNullException.ThrowIfNull(builder);

        // Expand ~ to home directory
        secretsPath = ExpandPath(secretsPath);
        keyPath = ExpandPath(keyPath);

        return builder.Add(new SecureStoreConfigurationSource
        {
            SecretsPath = secretsPath,
            KeyPath = keyPath
        });
    }

    /// <summary>
    /// Expands tilde (~) in path to user's home directory.
    /// Supports both Unix-style (/) and Windows-style (\) path separators.
    /// </summary>
    /// <param name="path">Path that may contain tilde prefix.</param>
    /// <returns>Expanded path.</returns>
    internal static string ExpandPath(string path)
    {
        if (string.IsNullOrEmpty(path))
        {
            return path;
        }

        // Handle exactly "~" - just the home directory
        if (path == "~")
        {
            return Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        }

        // Handle "~/" or "~\" - home directory with relative path (cross-platform)
        if (path.Length >= 2 && path[0] == '~' && (path[1] == '/' || path[1] == '\\'))
        {
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            return Path.Combine(home, path.Substring(2)); // Skip "~/" or "~\"
        }

        // Path doesn't start with ~/ or ~\ - return as-is
        // Note: ~username paths are not supported (Unix-specific, rarely used in .NET)
        return path;
    }
}
