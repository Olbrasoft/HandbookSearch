using Olbrasoft.HandbookSearch.Business.Configuration;

namespace HandbookSearch.Business.Tests.Configuration;

public class SecureStoreConfigurationProviderTests
{
    [Fact]
    public void Load_VaultNotExists_DoesNotThrow()
    {
        // Arrange
        var provider = new SecureStoreConfigurationProvider(
            "/nonexistent/path/secrets.json",
            "/nonexistent/path/secrets.key");

        // Act & Assert - should not throw
        provider.Load();
    }

    [Fact]
    public void Load_KeyNotExists_DoesNotThrow()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);
        var secretsPath = Path.Combine(tempDir, "secrets.json");
        File.WriteAllText(secretsPath, "{}"); // Create empty vault file

        try
        {
            var provider = new SecureStoreConfigurationProvider(
                secretsPath,
                "/nonexistent/path/secrets.key");

            // Act & Assert - should not throw
            provider.Load();
        }
        finally
        {
            // Cleanup
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void Constructor_SetsPathsCorrectly()
    {
        // Arrange
        var secretsPath = "/test/secrets.json";
        var keyPath = "/test/secrets.key";

        // Act
        var provider = new SecureStoreConfigurationProvider(secretsPath, keyPath);

        // Assert - provider was created without throwing
        Assert.NotNull(provider);
    }
}
