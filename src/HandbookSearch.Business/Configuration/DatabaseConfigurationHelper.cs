using Microsoft.Extensions.Configuration;

namespace Olbrasoft.HandbookSearch.Business.Configuration;

/// <summary>
/// Helper class for building database connection strings from configuration.
/// </summary>
public static class DatabaseConfigurationHelper
{
    /// <summary>
    /// Builds a PostgreSQL connection string from configuration section.
    /// Password is loaded from SecureStore (Database:Password key) if available.
    /// If not present, the connection string is built without a password (e.g. for local development).
    /// </summary>
    /// <param name="dbConfig">Configuration section containing Database settings (Host, Name, Username, Password).</param>
    /// <returns>PostgreSQL connection string.</returns>
    public static string BuildConnectionString(IConfigurationSection dbConfig)
    {
        ArgumentNullException.ThrowIfNull(dbConfig);

        var host = dbConfig["Host"] ?? "localhost";
        var database = dbConfig["Name"] ?? "handbook_search";
        var username = dbConfig["Username"] ?? "postgres";
        var password = dbConfig["Password"]; // From SecureStore if configured

        var connStr = $"Host={host};Database={database};Username={username}";

        if (!string.IsNullOrEmpty(password))
        {
            connStr += $";Password={password}";
        }

        return connStr;
    }
}
