using Microsoft.Extensions.Configuration;
using Olbrasoft.HandbookSearch.Business.Configuration;

namespace HandbookSearch.Business.Tests.Configuration;

public class SecureStoreConfigurationExtensionsTests
{
    [Fact]
    public void ExpandPath_NullOrEmpty_ReturnsAsIs()
    {
        // Act & Assert
        Assert.Null(SecureStoreConfigurationExtensions.ExpandPath(null!));
        Assert.Equal("", SecureStoreConfigurationExtensions.ExpandPath(""));
    }

    [Fact]
    public void ExpandPath_TildeOnly_ReturnsHomeDirectory()
    {
        // Arrange
        var expected = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        // Act
        var result = SecureStoreConfigurationExtensions.ExpandPath("~");

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void ExpandPath_TildeSlashPath_ExpandsToHomeDirectory()
    {
        // Arrange
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var expected = Path.Combine(home, ".config", "handbook-search", "secrets.json");

        // Act
        var result = SecureStoreConfigurationExtensions.ExpandPath("~/.config/handbook-search/secrets.json");

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void ExpandPath_AbsolutePath_ReturnsAsIs()
    {
        // Arrange - use platform-appropriate absolute path
        var absolutePath = Path.IsPathRooted("/var/lib/secrets.json")
            ? "/var/lib/secrets.json"  // Unix
            : @"C:\ProgramData\secrets.json";  // Windows

        // Act
        var result = SecureStoreConfigurationExtensions.ExpandPath(absolutePath);

        // Assert
        Assert.Equal(absolutePath, result);
    }

    [Fact]
    public void ExpandPath_TildeBackslashPath_ExpandsToHomeDirectory()
    {
        // Arrange - Windows-style path with backslash
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        // Act
        var result = SecureStoreConfigurationExtensions.ExpandPath(@"~\.config\handbook-search\secrets.json");

        // Assert - verify tilde is expanded to home directory
        Assert.StartsWith(home, result);
        Assert.Contains("handbook-search", result);
        Assert.Contains("secrets.json", result);
    }

    [Fact]
    public void ExpandPath_RelativePath_ReturnsAsIs()
    {
        // Arrange
        var relativePath = "config/secrets.json";

        // Act
        var result = SecureStoreConfigurationExtensions.ExpandPath(relativePath);

        // Assert
        Assert.Equal(relativePath, result);
    }

    [Fact]
    public void ExpandPath_TildeInMiddle_ReturnsAsIs()
    {
        // Arrange - tilde not at start should not be expanded
        var path = "/some/path/~user/file.json";

        // Act
        var result = SecureStoreConfigurationExtensions.ExpandPath(path);

        // Assert
        Assert.Equal(path, result);
    }

    [Fact]
    public void DefaultSecretsPath_ContainsHandbookSearch()
    {
        // Assert
        Assert.Contains("handbook-search", SecureStoreConfigurationExtensions.DefaultSecretsPath);
        Assert.Contains("secrets.json", SecureStoreConfigurationExtensions.DefaultSecretsPath);
    }

    [Fact]
    public void DefaultKeyPath_ContainsHandbookSearch()
    {
        // Assert
        Assert.Contains("handbook-search", SecureStoreConfigurationExtensions.DefaultKeyPath);
        Assert.Contains("secrets.key", SecureStoreConfigurationExtensions.DefaultKeyPath);
    }

    [Fact]
    public void AddSecureStore_NullBuilder_ThrowsArgumentNullException()
    {
        // Arrange
        IConfigurationBuilder? builder = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => builder!.AddSecureStore());
    }

    [Fact]
    public void AddSecureStore_WithPaths_NullBuilder_ThrowsArgumentNullException()
    {
        // Arrange
        IConfigurationBuilder? builder = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => builder!.AddSecureStore("/path/secrets.json", "/path/secrets.key"));
    }

    [Fact]
    public void AddSecureStore_AddsConfigurationSource()
    {
        // Arrange
        var builder = new ConfigurationBuilder();

        // Act
        var result = builder.AddSecureStore("/nonexistent/secrets.json", "/nonexistent/secrets.key");

        // Assert
        Assert.Same(builder, result);
        Assert.Single(builder.Sources);
        Assert.IsType<SecureStoreConfigurationSource>(builder.Sources[0]);
    }

    [Fact]
    public void AddSecureStore_DefaultPaths_AddsConfigurationSource()
    {
        // Arrange
        var builder = new ConfigurationBuilder();

        // Act
        var result = builder.AddSecureStore();

        // Assert
        Assert.Same(builder, result);
        Assert.Single(builder.Sources);
    }

    [Fact]
    public void AddSecureStore_ExpandsTildePaths()
    {
        // Arrange
        var builder = new ConfigurationBuilder();
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        // Act
        builder.AddSecureStore("~/secrets.json", "~/secrets.key");

        // Assert
        var source = builder.Sources[0] as SecureStoreConfigurationSource;
        Assert.NotNull(source);
        Assert.StartsWith(home, source.SecretsPath);
        Assert.StartsWith(home, source.KeyPath);
    }
}
