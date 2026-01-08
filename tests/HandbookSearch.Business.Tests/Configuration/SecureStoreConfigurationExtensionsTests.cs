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
        // Arrange
        var absolutePath = "/var/lib/handbook-search/secrets.json";

        // Act
        var result = SecureStoreConfigurationExtensions.ExpandPath(absolutePath);

        // Assert
        Assert.Equal(absolutePath, result);
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
}
